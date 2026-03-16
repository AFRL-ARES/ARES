using Ares.Datamodel;
using Ares.Datamodel.Device;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Factories;
using Ares.Device;
using Ares.Toolkit.Serial;
using ChemyxPumpPlugin.Commands;
using ChemyxPumpPlugin.Enums;
using ChemyxPumpPlugin.Responses;
using ChemyxPumpPlugin.Simulation;
using Google.Protobuf.WellKnownTypes;
using ReactiveUI;
using System.IO.Ports;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace ChemyxPumpPlugin;

public class ChemyxPump : AresDevice, IChemyxPump
{
  private readonly BehaviorSubject<AresStruct> _stateSubject = new(new AresStruct());
  private readonly IAresSerialConnection _connection;
  private Task _pollingTask = Task.CompletedTask;
  private CancellationTokenSource _pollingCancellation = new();
  private ViewParametersResponse? _cachedParameters;

  public ChemyxPump(DeviceConnectionInfo connectionInfo) : base(connectionInfo)
  {
    var serialInfo = connectionInfo.SerialConnectionInfo;
    _connection = connectionInfo.Simulated 
      ? new SimChemyxConnection(serialInfo.PortName)
      : new AresHardwareConnection(new SerialPortConnectionInfo(9600, Parity.None, 8, StopBits.One), serialInfo.PortName);

    DualPump = connectionInfo.DeviceSettings.Fields.GetValueOrDefault("DualPump")?.BoolValue ?? false;
  }

  public override async Task<bool> Activate(CancellationToken ct)
  {
    try
    {
      _cachedParameters = await _connection.Send(new ViewParameterCommand());
      StartPolling();
      Status = new DeviceOperationalStatus { OperationalState = OperationalState.Active, Message = "Chemyx Pump is active" };
      return true;
    }
    catch(Exception e)
    {
      Status = new DeviceOperationalStatus { OperationalState = OperationalState.Error, Message = $"Failed to activate: {e.Message}" };
      return false;
    }
  }

  public override async Task EnterSafeMode(CancellationToken ct)
  {
    await _connection.Send(new StopCommand(null));
  }

  public override Task<AresStruct> GetState() => Task.FromResult(_stateSubject.Value);
  public override IObservable<AresStruct> StateStream => _stateSubject.AsObservable();

  private void StartPolling()
  {
    _pollingCancellation.Cancel();
    _pollingCancellation = new CancellationTokenSource();
    _pollingTask = Task.Run(async () =>
    {
      while(!_pollingCancellation.Token.IsCancellationRequested)
      {
        try
        {
          await UpdateStateFromDevice();
        }
        catch { /* Ignore poll errors */ }
        await Task.Delay(TimeSpan.FromMilliseconds(750), _pollingCancellation.Token);
      }
    }, _pollingCancellation.Token);
  }

  private async Task UpdateStateFromDevice()
  {
    var pumpCount = DualPump ? 2 : 1;
    var pumpStates = new List<AresValue>();

    for(int i = 1; i <= pumpCount; i++)
    {
      var status = await _connection.Send(new PumpStatusCommand(i));
      var disp = await _connection.Send(new DispensedVolumeCommand(i));
      var elapsed = await _connection.Send(new ElapsedTimeCommand(i));

      var config = i == 1 ? _cachedParameters?.Pump1 : _cachedParameters?.Pump2;

      var builder = AresStateBuilder.Create()
        .Add("Index", i)
        .Add("Status", status.Status?.ToString() ?? "Unknown")
        .Add("Volume", disp.Value ?? 0)
        .Add("Time", elapsed.Value.HasValue ? TimeSpan.FromMinutes(elapsed.Value.Value).ToString(@"hh\:mm\:ss") : "00:00:00");

      if(config != null)
      {
        builder.Add("Diameter", config.Diameter);
        builder.Add("TargetVolume", config.Volume);
        builder.Add("Rate", config.Rate);
        builder.Add("Delay", config.Delay);
        builder.Add("Units", config.Units.ToString());
      }

      pumpStates.Add(new AresValue { StructValue = builder.Build() });
    }

    var rootBuilder = AresStateBuilder.Create()
      .Add("Name", Name)
      .Add("DualPump", DualPump)
      .AddList("Pumps", pumpStates, v => v);

    _stateSubject.OnNext(rootBuilder.Build());
  }

  public override async Task UpdateSettings(AresStruct settings)
  {
    DualPump = settings.Fields.GetValueOrDefault("DualPump")?.BoolValue ?? false;
    await Task.CompletedTask;
  }

  public override Task<AresStruct> GetSettings()
  {
    return Task.FromResult(AresStateBuilder.Create().Add("DualPump", DualPump).Build());
  }

  protected override Task<List<DeviceCommandDescriptor>> BuildCommandDescriptorsAsync()
  {
    return Task.FromResult(new List<DeviceCommandDescriptor>
    {
      new()
      {
        Name = ChemyxPumpCommand.Start.ToString(),
        Description = "Starts the pump.",
        InputSchema = AresSchemaBuilder.Empty()
          .AddEntry("PumpIndex", AresSchemaBuilder.NumberEntry().Build())
          .AddEntry("Mode", AresSchemaBuilder.NumberEntry().AsOptional().Build())
          .Build()
      },
      new()
      {
        Name = ChemyxPumpCommand.Stop.ToString(),
        Description = "Stops the pump.",
        InputSchema = AresSchemaBuilder.Empty()
          .AddEntry("PumpIndex", AresSchemaBuilder.NumberEntry().Build())
          .Build()
      },
      new()
      {
        Name = ChemyxPumpCommand.Pause.ToString(),
        Description = "Pauses the pump.",
        InputSchema = AresSchemaBuilder.Empty()
          .AddEntry("PumpIndex", AresSchemaBuilder.NumberEntry().Build())
          .Build()
      },
      new()
      {
        Name = ChemyxPumpCommand.SetRate.ToString(),
        Description = "Sets the flow rate.",
        InputSchema = AresSchemaBuilder.Empty()
          .AddEntry("PumpIndex", AresSchemaBuilder.NumberEntry().Build())
          .AddEntry("Rate", AresSchemaBuilder.NumberEntry().Build())
          .Build()
      },
      new()
      {
        Name = ChemyxPumpCommand.SetVolume.ToString(),
        Description = "Sets the target volume.",
        InputSchema = AresSchemaBuilder.Empty()
          .AddEntry("PumpIndex", AresSchemaBuilder.NumberEntry().Build())
          .AddEntry("Volume", AresSchemaBuilder.NumberEntry().Build())
          .Build()
      },
      new()
      {
        Name = ChemyxPumpCommand.SetDiameter.ToString(),
        Description = "Sets the inner diameter of the syringe.",
        InputSchema = AresSchemaBuilder.Empty()
          .AddEntry("PumpIndex", AresSchemaBuilder.NumberEntry().Build())
          .AddEntry("Diameter", AresSchemaBuilder.NumberEntry().Build())
          .Build(),
        OutputSchema = AresSchemaBuilder.NumberEntry().Build()
      },
      new()
      {
        Name = ChemyxPumpCommand.SetUnits.ToString(),
        Description = "Sets the measurement units for the pump.",
        InputSchema = AresSchemaBuilder.Empty()
          .AddEntry("PumpIndex", AresSchemaBuilder.NumberEntry().Build())
          .AddEntry(ChemyxParameter.Units.ToString(), AresSchemaBuilder.NumberEntry().Build())
          .Build(),
        OutputSchema = AresSchemaBuilder.NumberEntry().Build()
      },
      new()
      {
        Name = ChemyxPumpCommand.SetDelay.ToString(),
        Description = "Sets the start delay time in seconds.",
        InputSchema = AresSchemaBuilder.Empty()
          .AddEntry("PumpIndex", AresSchemaBuilder.NumberEntry().Build())
          .AddEntry(ChemyxParameter.Delay.ToString(), AresSchemaBuilder.NumberEntry().Build())
          .Build(),
        OutputSchema = AresSchemaBuilder.NumberEntry().Build()
      },
      new()
      {
        Name = ChemyxPumpCommand.SetTime.ToString(),
        Description = "Sets the runtime in minutes.",
        InputSchema = AresSchemaBuilder.Empty()
          .AddEntry("PumpIndex", AresSchemaBuilder.NumberEntry().Build())
          .AddEntry(ChemyxParameter.Time.ToString(), AresSchemaBuilder.NumberEntry().Build())
          .Build(),
        OutputSchema = AresSchemaBuilder.Entry(AresDataType.Struct)
          .WithStructSchema(s => s
            .AddEntry("Rate", AresDataType.Number, false)
            .AddEntry("Time", AresDataType.Number, false))
          .Build()
      },
      new()
      {
        Name = ChemyxPumpCommand.GetDispensedVolume.ToString(),
        Description = "Gets the volume dispensed by the pump.",
        InputSchema = AresSchemaBuilder.Empty()
          .AddEntry("PumpIndex", AresSchemaBuilder.NumberEntry().Build())
          .Build(),
        OutputSchema = AresSchemaBuilder.NumberEntry().Build()
      },
      new()
      {
        Name = ChemyxPumpCommand.GetElapsedTime.ToString(),
        Description = "Gets the elapsed time since the pump started running.",
        InputSchema = AresSchemaBuilder.Empty()
          .AddEntry("PumpIndex", AresSchemaBuilder.NumberEntry().Build())
          .Build(),
        OutputSchema = AresSchemaBuilder.NumberEntry().Build()
      },
      new()
      {
        Name = ChemyxPumpCommand.GetLimitParameter.ToString(),
        Description = "Gets the hardware limit parameters of the pump.",
        InputSchema = AresSchemaBuilder.Empty()
          .AddEntry("PumpIndex", AresSchemaBuilder.NumberEntry().Build())
          .Build(),
        OutputSchema = AresSchemaBuilder.Entry(AresDataType.Struct)
          .WithStructSchema(s => s
            .AddEntry("MaxRate", AresDataType.Number, false)
            .AddEntry("MinRate", AresDataType.Number, false)
            .AddEntry("MaxVolume", AresDataType.Number, false)
            .AddEntry("MinVolume", AresDataType.Number, false))
          .Build()
      }
    });
  }

  private async Task<(double rate, TimeSpan time)?> SetTime(TimeSpan time, int? pump = null)
  {
    var totalMinutes = time.TotalMinutes;
    var response = await _connection.Send(new SetTimeCommand(pump ?? 1, totalMinutes), TimeSpan.FromSeconds(5));
    if(response is null || !response.Rate.HasValue || !response.Time.HasValue)
      return null;

    _cachedParameters = await _connection.Send(new ViewParameterCommand());
    var responseTimespan = TimeSpan.FromMinutes(response.Time.Value);
    return (response.Rate.Value, responseTimespan);
  }

  public override async Task<CommandResult> ExecuteCommand(string command, List<DeviceCommandArgument> arguments, CancellationToken token)
  {
    if(!System.Enum.TryParse<ChemyxPumpCommand>(command, out var chemyxCommand))
      return new CommandResult { Success = false, Error = $"Unknown command: {command}" };

    var result = new CommandResult();

    try
    {
      int? pumpIdx = (int?)arguments.FirstOrDefault(a => a.ArgName == "PumpIndex")?.ArgValue.NumberValue;

      switch(chemyxCommand)
      {
        case ChemyxPumpCommand.Start:
          int mode = (int)(arguments.FirstOrDefault(a => a.ArgName == "Mode")?.ArgValue.NumberValue ?? 0);
          await _connection.Send(new StartCommand(pumpIdx, mode));
          break;

        case ChemyxPumpCommand.Stop:
          await _connection.Send(new StopCommand(pumpIdx));
          break;

        case ChemyxPumpCommand.Pause:
          await _connection.Send(new PauseCommand(pumpIdx));
          break;

        case ChemyxPumpCommand.SetRate:
          var rateArg = arguments.FirstOrDefault(a => a.ArgName == "Rate");
          if(rateArg != null && rateArg.ArgValue.HasNumberValue)
          {
            await _connection.Send(new SetRateCommand(pumpIdx ?? 1, rateArg.ArgValue.NumberValue));
            _cachedParameters = await _connection.Send(new ViewParameterCommand());
          }

          break;

        case ChemyxPumpCommand.SetVolume:
          var volArg = arguments.FirstOrDefault(a => a.ArgName == "Volume");
          if(volArg != null && volArg.ArgValue.HasNumberValue)
          {
            await _connection.Send(new SetVolumeCommand(pumpIdx ?? 1, volArg.ArgValue.NumberValue));
            _cachedParameters = await _connection.Send(new ViewParameterCommand());
          }

          result.Success = true;
          break;

        case ChemyxPumpCommand.SetDiameter:
          var diameterArg = arguments.FirstOrDefault(a => a.ArgName == "Diameter");
          NumericResponse? response;

          if(diameterArg is not null && diameterArg.ArgValue.HasNumberValue)
          {
            response = await _connection.Send(new SetDiameterCommand(pumpIdx ?? 1, diameterArg.ArgValue.NumberValue));
            result.Result = response.Value is not null ? AresValueHelper.CreateNumber((int)response.Value) : AresValueHelper.CreateNull();
            result.Success = !result.Result.HasNullValue;
            return result;
          }

          return Failure($"Could not execute the Set Diamter command on {Name}, as the diameter argument was either not provided or malformed.");

        case ChemyxPumpCommand.SetUnits:
          var value = GetNumberParam(ChemyxParameter.Units.ToString(), arguments);
          if(!value.HasValue)
            return Failure("SetUnits requires a numeric Value parameter.");

          var units = (PumpUnits)(int)value.Value;
          var setUnitsResponse = await _connection.Send(new SetUnitsCommand(pumpIdx ?? 1, units));
          result.Result = setUnitsResponse.Value is not null ? AresValueHelper.CreateNumber((double)setUnitsResponse.Value) : AresValueHelper.CreateNull();
          result.Success = !result.Result.HasNullValue;
          return result;

        case ChemyxPumpCommand.SetDelay:
          var delayValue = GetNumberParam(ChemyxParameter.Delay.ToString(), arguments);
          if(!delayValue.HasValue)
            return Failure("SetDelay requires a numeric Value parameter.");
          var delayReq = TimeSpan.FromSeconds(delayValue.Value);
          var setDelayResponse = await _connection.Send(new SetDelayCommand(pumpIdx ?? 1, delayValue.Value));
          result.Result = setDelayResponse.Value is not null ? AresValueHelper.CreateNumber((double)setDelayResponse.Value) : AresValueHelper.CreateNull();
          result.Success = !result.Result.HasNullValue;
          return result;

        case ChemyxPumpCommand.SetTime:
          var timeValue = GetNumberParam(ChemyxParameter.Time.ToString(), arguments);
          if(!timeValue.HasValue)
            return Failure("SetTime requires a numeric Value parameter.");

          var timeSpan = TimeSpan.FromMinutes(timeValue.Value);
          var setTimeResponse = await SetTime(timeSpan, pumpIdx);
          if(setTimeResponse.HasValue)
          {
            var responseTime = setTimeResponse.Value.time.TotalMinutes;
            var valueStruct = AresValueHelper.CreateStruct();
            valueStruct.StructValue.AddNumber("Rate", setTimeResponse.Value.rate).AddNumber("Time", responseTime);
            result.Result = valueStruct; 
          }
          else
            result.Result = AresValueHelper.CreateNull();
          
          return result;

        case ChemyxPumpCommand.GetDispensedVolume:
          var dispensedVolumeResponse = await _connection.Send(new DispensedVolumeCommand(pumpIdx ?? 1), TimeSpan.FromSeconds(5));
          result.Result = dispensedVolumeResponse.Value is not null ? AresValueHelper.CreateNumber((double)dispensedVolumeResponse.Value) : AresValueHelper.CreateNull();
          result.Success = !result.Result.HasNullValue;
          return result;

        case ChemyxPumpCommand.GetElapsedTime:
          var elapsedTimeResponse = await _connection.Send(new ElapsedTimeCommand(pumpIdx ?? 1), TimeSpan.FromSeconds(5));
          result.Result = elapsedTimeResponse.Value is not null ? AresValueHelper.CreateNumber((double)elapsedTimeResponse.Value) : AresValueHelper.CreateNull();
          result.Success = !result.Result.HasNullValue;
          return result;

        case ChemyxPumpCommand.GetLimitParameter:
          var limitResponse = await _connection.Send(new ReadLimitParameterCommand(pumpIdx ?? 1, 0), TimeSpan.FromSeconds(5));
          if(limitResponse is null)
          {
            result.Result = AresValueHelper.CreateNull();
            break;
          }

          var limitValueStruct = AresValueHelper.CreateStruct();
          limitValueStruct.StructValue.AddNumber("MaxRate", limitResponse.MaxRate)
            .AddNumber("MinRate", limitResponse.MinRate)
            .AddNumber("MaxVolume", limitResponse.MaxVolume)
            .AddNumber("MinVolume", limitResponse.MinVolume);

          result.Result = limitValueStruct;
          result.Success = true;
          return result;

        default:
          return new CommandResult { Success = false, Error = "Not implemented" };
      }
      return new CommandResult { Success = true };
    }

    catch(Exception e)
    {
      return new CommandResult { Success = false, Error = e.Message };
    }
  }

  private double? GetNumberParam(string name, List<DeviceCommandArgument> arguments, double? fallback = null)
  {
    var param = arguments.FirstOrDefault(p => p.ArgName == name);
    return param != null && param.ArgValue.HasNumberValue ? param.ArgValue.NumberValue : fallback;
  }
  private CommandResult Failure(string message) => new CommandResult { Success = false, Error = message };

  public async ValueTask DisposeAsync()
  {
    _pollingCancellation.Cancel();
    await _pollingTask;
    await _connection.DisposeAsync();
    _stateSubject.OnCompleted();
  }

  public bool DualPump { get; private set; }

  public double[]? DispensedVolumes { get; private set; }
}
