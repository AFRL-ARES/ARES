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
      new() { Name = ChemyxPumpCommand.Start.ToString(), Description = "Starts the pump." },
      new() { Name = ChemyxPumpCommand.Stop.ToString(), Description = "Stops the pump." },
      new() { Name = ChemyxPumpCommand.Pause.ToString(), Description = "Pauses the pump." },
      new() {
        Name = ChemyxPumpCommand.SetRate.ToString(),
        Description = "Sets the flow rate.",
        InputSchema = AresSchemaBuilder.Empty()
          .AddEntry("PumpIndex", AresSchemaBuilder.NumberEntry().Build())
          .AddEntry("Rate", AresSchemaBuilder.NumberEntry().Build())
          .Build()
      },
      new() {
        Name = ChemyxPumpCommand.SetVolume.ToString(),
        Description = "Sets the target volume.",
        InputSchema = AresSchemaBuilder.Empty()
          .AddEntry("PumpIndex", AresSchemaBuilder.NumberEntry().Build())
          .AddEntry("Volume", AresSchemaBuilder.NumberEntry().Build())
          .Build()
      }
    });
  }

  public override async Task<CommandResult> ExecuteCommand(string command, List<DeviceCommandArgument> arguments, CancellationToken token)
  {
    if(!Enum.TryParse<ChemyxPumpCommand>(command, out var chemyxCommand))
      return new CommandResult { Success = false, Error = $"Unknown command: {command}" };

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
          break;
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

  public async ValueTask DisposeAsync()
  {
    _pollingCancellation.Cancel();
    await _pollingTask;
    await _connection.DisposeAsync();
    _stateSubject.OnCompleted();
  }

  public bool DualPump { get; private set; }
}
