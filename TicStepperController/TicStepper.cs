using Ares.Datamodel;
using Ares.Datamodel.Device;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Factories;
using Ares.Device;
using Ares.Toolkit.Serial;
using System.IO.Ports;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using TicStepperController.Commands;
using TicStepperController.Enums;
using TicStepperController.Responses;
using TicStepperController.Simulation;

namespace TicStepperController;

public class TicStepper : AresDevice, ITicStepperController
{
  private readonly BehaviorSubject<AresStruct> _stateSubject = new(new AresStruct());
  private readonly IAresSerialConnection _connection;
  private Task _stateUpdater = Task.CompletedTask;
  private CancellationTokenSource _stateUpdaterCancellation = new();

  public TicStepper(DeviceConnectionInfo connectionInfo) : base(connectionInfo)
  {
    var serialInfo = connectionInfo.SerialConnectionInfo;
    if(connectionInfo.Simulated)
      _connection = new SimTicConnection(serialInfo.PortName);
    
    else
      _connection = new AresHardwareConnection(new SerialPortConnectionInfo(9600, Parity.None, 8, StopBits.One), serialInfo.PortName);

    LoadSettings(connectionInfo.DeviceSettings);
    StateStream = _stateSubject.AsObservable();
  }

  private void LoadSettings(AresStruct settings)
  {
    UserStepSize = (uint)(settings.Fields.GetValueOrDefault("CustomStepSize")?.NumberValue ?? 1);
    SmartStepCalculation = settings.Fields.GetValueOrDefault("DynamicStepCalculation")?.BoolValue ?? false;
    InitialSpoolRadius = settings.Fields.GetValueOrDefault("SpoolRadius")?.NumberValue;
    FilterPaperThickness = settings.Fields.GetValueOrDefault("FilterPaperThickness")?.NumberValue;
    IdealLinearStepSize = settings.Fields.GetValueOrDefault("IdealLinearStepSize")?.NumberValue;
    StepAngle = settings.Fields.GetValueOrDefault("StepAngle")?.NumberValue ?? 1.8;
  }

  public override async Task<bool> Activate(CancellationToken ct)
  {
    try
    {
      await _connection.Send(new ExitSafeStartCommand());
      await UpdateStateFromDevice();
      StartStateUpdater();
      Status = new DeviceOperationalStatus { OperationalState = OperationalState.Active, Message = "Tic Stepper is active" };
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
    await _connection.Send(new EnterSafeStartCommand());
    await _connection.Send(new DeEnergizeCommand());
  }

  public override Task<AresStruct> GetState() => Task.FromResult(_stateSubject.Value);

  private void StartStateUpdater()
  {
    _stateUpdaterCancellation.Cancel();
    _stateUpdaterCancellation = new CancellationTokenSource();
    _stateUpdater = Task.Run(async () =>
    {
      while (!_stateUpdaterCancellation.Token.IsCancellationRequested)
      {
        try
        {
          await UpdateStateFromDevice();
        }
        catch{ /* Ignore update errors */ }
        await Task.Delay(500, _stateUpdaterCancellation.Token);
      }
    }, _stateUpdaterCancellation.Token);
  }

  private async Task UpdateStateFromDevice()
  {
    var opState = await _connection.Send(new GetOperationStateRequest());
    var pos = await _connection.Send(new GetCurrentPositionRequest());
    var target = await _connection.Send(new GetTargetPositionRequest());
    var flags = await _connection.Send(new GetMiscFlagsRequest());
    var errorsResponse = await _connection.Send(new GetErrorStatusRequest());
    var mode = await _connection.Send(new GetStepModeRequest());
    var maxAcceleration = await _connection.Send(new GetMaxAccelerationRequest());
    var maxDeceleration = await _connection.Send(new GetMaxDecelerationRequest());
    var maxSpeed = await _connection.Send(new GetMaxSpeedRequest());
    var limit = await _connection.Send(new GetCurrentLimitRequest());
    var startingSpeed = await _connection.Send(new GetStartingSpeedRequest());
    var stepMode = await _connection.Send(new GetStepModeRequest());

    CalculateMicroStepAngle(mode.StepMode);
    if(SmartStepCalculation) 
      CalculateCurrentRadius();

    var next = AresStateBuilder.From(_stateSubject.Value)
      .Add("OperationState", opState.State.ToString())
      .Add("CurrentPosition", pos.Position)
      .Add("TargetPosition", target.Position)
      .Add("StepMode", mode.StepMode.ToString())
      .Add("MaxAcceleration", maxAcceleration.Value)
      .Add("MaxDeceleration", maxDeceleration.Value)
      .Add("MaxSpeed", maxSpeed.Value)
      .Add("CurrentLimit", limit.Value)
      .Add("CustomStepSize", UserStepSize)
      .Add("StepMode", stepMode.StepMode.ToString())
      .Add("StartingSpeed", startingSpeed.Value)
      .AddStruct("MiscFlags", miscFlags => miscFlags
        .Add("Energized", flags.Energized)
        .Add("PositionUncertain", flags.PositionUncertain)
        .Add("ForwardLimitActive", flags.ForwardLimitActive)
        .Add("ReverseLimitActive", flags.ReverseLimitActive)
        .Add("HomingActive", flags.HomingActive))
      .AddStruct("Errors", errors => errors
        .Add("IntentionallyDeEnergized", errorsResponse.IntentionallyDeEnergized)
        .Add("MotorDriverError", errorsResponse.MotorDriverError)
        .Add("LowVin", errorsResponse.LowVin)
        .Add("KillSwitchActive", errorsResponse.KillSwitchActive)
        .Add("RequiredInputsInvalid", errorsResponse.RequiredInputInvalid)
        .Add("SerialError", errorsResponse.SerialError)
        .Add("CommandTimeout", errorsResponse.CommandTimeout)
        .Add("SafeStartViolation", errorsResponse.SafeStartViolation)
        .Add("ErrLineHigh", errorsResponse.ErrLineHigh))
      .Build();

    _stateSubject.OnNext(next);
  }

  private void CalculateMicroStepAngle(StepMode stepMode)
  {
    double angle = StepAngle;
    MicroStepAngle = stepMode switch
    {
      StepMode.Step1_2 => angle / 2.0,
      StepMode.Step1_4 => angle / 4.0,
      StepMode.Step1_8 => angle / 8.0,
      StepMode.Step1_16 => angle / 16.0,
      StepMode.Step1_32 => angle / 32.0,
      StepMode.Step1_64 => angle / 64.0,
      StepMode.Step1_128 => angle / 128.0,
      StepMode.Step1_256 => angle / 256.0,
      _ => angle
    };
  }

  private void CalculateCurrentRadius()
  {
    if(InitialSpoolRadius is not null && FilterPaperThickness is not null)
    {
      var displacementInDegrees = TotalSpoolDisplacementInMicrosteps * MicroStepAngle;
      CurrentSpoolRadius = InitialSpoolRadius.Value + (FilterPaperThickness.Value * Math.Floor(displacementInDegrees / 360.0));
    }
  }

  public override async Task UpdateSettings(AresStruct settings)
  {
    LoadSettings(settings);
    await Task.CompletedTask;
  }

  public override Task<AresStruct> GetSettings()
  {
    return Task.FromResult(AresStateBuilder.Create()
      .Add("CustomStepSize", UserStepSize)
      .Add("DynamicStepCalculation", SmartStepCalculation)
      .Add("SpoolRadius", InitialSpoolRadius ?? 0)
      .Add("FilterPaperThickness", FilterPaperThickness ?? 0)
      .Add("IdealLinearStepSize", IdealLinearStepSize ?? 0)
      .Add("StepAngle", StepAngle)
      .Build());
  }

  protected override Task<List<DeviceCommandDescriptor>> BuildCommandDescriptorsAsync()
  {
    return Task.FromResult(new List<DeviceCommandDescriptor>
    {
      new() { Name = TicStepperCommand.Reset.ToString(), Description = "Resets the device." },
      new() { Name = TicStepperCommand.Energize.ToString(), Description = "Energizes the motor." },
      new() { Name = TicStepperCommand.DeEnergize.ToString(), Description = "De-energizes the motor." },
      new() { Name = TicStepperCommand.EnterSafeStart.ToString(), Description = "Enters safe start mode." },
      new() { Name = TicStepperCommand.ExitSafeStart.ToString(), Description = "Exits safe start mode." },
      new() { Name = TicStepperCommand.HaltAndHold.ToString(), Description = "Halts and holds the motor." },
      new() { Name = TicStepperCommand.NextStep.ToString(), Description = "Moves to the next step." },
      new() { Name = TicStepperCommand.PreviousStep.ToString(), Description = "Moves to the previous step." },
      new() { Name = TicStepperCommand.HalfStep.ToString(), Description = "Moves half a step." },
      new() {
        Name = TicStepperCommand.SetTargetPosition.ToString(),
        Description = "Sets target position.",
        InputSchema = AresSchemaBuilder.Empty().AddEntry("Position", AresSchemaBuilder.NumberEntry().Build()).Build()
      },
      new() {
        Name = TicStepperCommand.HaltAndSetPosition.ToString(),
        Description = "Halts and sets current position.",
        InputSchema = AresSchemaBuilder.Empty().AddEntry("Position", AresSchemaBuilder.NumberEntry().Build()).Build()
      }
    });
  }

  public override async Task<CommandResult> ExecuteCommand(string command, List<DeviceCommandArgument> arguments, CancellationToken token)
  {
    if(!Enum.TryParse<TicStepperCommand>(command, out var ticCommand))
      return new CommandResult { Success = false, Error = $"Unknown command: {command}" };

    try
    {
      switch (ticCommand)
      {
        case TicStepperCommand.Reset:
          await _connection.Send(new ResetCommand());
          break;
        case TicStepperCommand.Energize:
          await _connection.Send(new EnergizeCommand());
          break;
        case TicStepperCommand.DeEnergize:
          await _connection.Send(new DeEnergizeCommand());
          break;
        case TicStepperCommand.EnterSafeStart:
          await _connection.Send(new EnterSafeStartCommand());
          break;
        case TicStepperCommand.ExitSafeStart:
          await _connection.Send(new ExitSafeStartCommand());
          break;
        case TicStepperCommand.HaltAndHold:
          await _connection.Send(new HaltAndHoldCommand());
          break;
        case TicStepperCommand.NextStep:
          await PerformStep(1.0);
          break;
        case TicStepperCommand.PreviousStep:
          await PerformStep(-1.0);
          break;
        case TicStepperCommand.HalfStep:
          await PerformStep(0.5);
          break;
        case TicStepperCommand.SetTargetPosition:
          var targetPosArg = arguments.FirstOrDefault(a => a.ArgName == "Position");
          if(targetPosArg != null && targetPosArg.ArgValue.HasNumberValue)
            await _connection.Send(new SetTargetPositionCommand((int)targetPosArg.ArgValue.NumberValue));
          
          break;
        case TicStepperCommand.HaltAndSetPosition:
          var haltPosArg = arguments.FirstOrDefault(a => a.ArgName == "Position");
          if(haltPosArg != null && haltPosArg.ArgValue.HasNumberValue)
            await _connection.Send(new HaltAndSetPositionCommand((int)haltPosArg.ArgValue.NumberValue));
          
          break;
        default:
          return new CommandResult { Success = false, Error = "Not implemented" };
      }
      return new CommandResult { Success = true };
    }
    catch (Exception e)
    {
      return new CommandResult { Success = false, Error = e.Message };
    }
  }

  private async Task PerformStep(double scale)
  {
    var posResponse = await _connection.Send(new GetCurrentPositionRequest());
    int currentPosition = posResponse.Position;
    int targetPosition;

    if(SmartStepCalculation && IdealLinearStepSize.HasValue)
    {
      CalculateCurrentRadius();
      var angularDisplacement = 180 * IdealLinearStepSize.Value / (Math.PI * CurrentSpoolRadius);
      var numberOfSteps = (angularDisplacement / MicroStepAngle) * scale;
      targetPosition = (int)(currentPosition + numberOfSteps);
      TotalSpoolDisplacementInMicrosteps += numberOfSteps;
    }
    else
    {
      targetPosition = (int)(currentPosition + (UserStepSize * scale));
    }

    await _connection.Send(new SetTargetPositionCommand(targetPosition));
  }

  public async ValueTask DisposeAsync()
  {
    _stateUpdaterCancellation.Cancel();
    await _stateUpdater;
    await _connection.DisposeAsync();
    _stateSubject.OnCompleted();
  }

  public uint UserStepSize { get; private set; } = 1;
  public bool SmartStepCalculation { get; private set; }
  public double? InitialSpoolRadius { get; private set; }
  public double? FilterPaperThickness { get; private set; }
  public double? IdealLinearStepSize { get; private set; }
  public double StepAngle { get; private set; } = 1.8;
  public double CurrentSpoolRadius { get; private set; }
  public double TotalSpoolDisplacementInMicrosteps { get; private set; } = 0;
  public double MicroStepAngle { get; private set; }
  public override IObservable<AresStruct> StateStream { get; }
}
