using Ares.Datamodel;
using Ares.Device;
using Ares.Device.Serial;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using TicStepperController.Commands;
using TicStepperController.Commands.Enums;
using TicStepperController.Commands.Responses;
using TicStepperController.Config;
using TicStepperController.Proto.Extensions;

namespace TicStepperController;
public class StepperController : SerialDevice<IStepperControllerConnection>, IStepperController
{
  private ISubject<Messaging.TicState> _stateSubject = new BehaviorSubject<Messaging.TicState>(new Messaging.TicState());
  private Task _stateUpdater = Task.CompletedTask;
  private CancellationTokenSource _stateUpdaterCancellation = new CancellationTokenSource();
  private readonly ILogger _logger;

  public StepperController(string name, IStepperControllerConnection connection, ILogger<IStepperController>? logger = null) : base(name, connection)
  {
    StateStream = _stateSubject.AsObservable();
    if(logger is not null)
      _logger = logger;
    else
      _logger = NullLogger<IStepperController>.Instance;
  }

  public async Task<OperationState> GetOperationState()
  {
    var response = await Connection.Send(new OperationStateRequest());
    return response.State;
  }

  public Task<CurrentPosition> GetCurrentPosition()
  {
    return Connection.Send(new CurrentPositionRequest());
  }

  protected override async Task<SerialDeviceValidationResult> Validate()
  {
    try
    {
      var operationResponse = await GetOperationState();
      return new SerialDeviceValidationResult(true);
    }
    catch(Exception e)
    {
      return new SerialDeviceValidationResult(false, e.Message);
    }
  }

  public override async Task EnterSafeMode(CancellationToken ct)
  {
    await EnterSafeStart();
  }

  public Task<ErrorsOccurred> GetErrorsOccurred()
  {
    return Connection.Send(new ErrorsOccurredRequest());
  }

  public Task<ErrorStatus> GetErrorStatus()
  {
    return Connection.Send(new ErrorStatusRequest());
  }

  public Task<MaxAcceleration> GetMaxAcceleration()
  {
    return Connection.Send(new MaxAccelerationRequest());
  }

  public Task<MaxDeceleration> GetMaxDeceleration()
  {
    return Connection.Send(new MaxDecelerationRequest());
  }

  public Task<CurrentLimit> GetCurrentLimit()
  {
    return Connection.Send(new CurrentLimitRequest());
  }

  public Task<MaxSpeed> GetMaxSpeed()
  {
    return Connection.Send(new MaxSpeedRequest());
  }

  public Task<MiscFlags> GetMiscFlags()
  {
    return Connection.Send(new MiscFlagsRequest());
  }

  public Task<StartingSpeed> GetStartingSpeed()
  {
    return Connection.Send(new StartingSpeedRequest());
  }

  public async Task<StepMode> GetStepMode()
  {
    var response = await Connection.Send(new StepModeRequest());
    return response.StepMode;
  }

  public Task<TargetPosition> GetTargetPosition()
  {
    return Connection.Send(new TargetPositionRequest());
  }

  public Task Reset()
  {
    return Connection.Send(new ResetCommand());
  }

  public Task Energize()
  {
    return Connection.Send(new EnergizeCommand());
  }

  public Task DeEnergize()
  {
    return Connection.Send(new DeEnergizeCommand());
  }

  public Task EnterSafeStart()
  {
    return Connection.Send(new EnterSafeStartCommand());
  }

  public Task ExitSafeStart()
  {
    return Connection.Send(new ExitSafeStartCommand());
  }

  public Task HaltAndHold()
  {
    return Connection.Send(new HaltAndHoldCommand());
  }

  public Task ResetCommandTimeout()
  {
    return Connection.Send(new ResetCommandTimeoutCommand());
  }

  public Task SetMaxAcceleration(uint acceleration)
  {
    return Connection.Send(new SetMaxAccelerationCommand(acceleration));
  }

  public Task SetMaxDeceleration(uint deceleration)
  {
    return Connection.Send(new SetMaxDecelerationCommand(deceleration));
  }

  public Task SetCurrentLimit(uint limit)
  {
    return Connection.Send(new SetCurrentLimitCommand(limit));
  }

  public Task SetMaxSpeed(uint speed)
  {
    return Connection.Send(new SetMaxSpeedCommand(speed));
  }

  public Task SetStartingSpeed(uint speed)
  {
    return Connection.Send(new SetStartingSpeedCommand(speed));
  }

  public Task SetStepMode(StepMode stepMode)
  {
    return Connection.Send(new SetStepModeCommand(stepMode));
  }

  public Task SetTargetPosition(int position)
  {
    return Connection.Send(new SetTargetPositionCommand(position));
  }

  public async Task HaltAndSetPosition(int position)
  {
    await Connection.Send(new HaltAndSetPositionCommand(position));
  }

  public async Task Init(StepperControllerConfig config)
  {
    if(config.MaxAcceleration.HasValue)
      await SetMaxAcceleration(config.MaxAcceleration.Value);

    if(config.MaxDeceleration.HasValue)
      await SetMaxDeceleration(config.MaxDeceleration.Value);

    if(config.MaxSpeed.HasValue)
      await SetMaxSpeed(config.MaxSpeed.Value);

    if(config.StartingSpeed.HasValue)
      await SetStartingSpeed(config.StartingSpeed.Value);

    if(config.StepMode != Messaging.StepMode.Undefined)
      await SetStepMode(config.StepMode.ToInternal());

    if(config.CurrentLimit.HasValue)
      await SetCurrentLimit(config.CurrentLimit.Value);

    UserStepSize = config.CustomStepSize ?? 1;
    SmartStepCalculation = config.DynamicStepCalculation;

    if(SmartStepCalculation)
    {
      //These are values we need only if the user requested that the device dynamically calculated the number of steps being taken
      InitialSpoolRadius = config.SpoolRadius;
      FilterPaperThickness = config.FilterPaperThickness;
      IdealLinearStepSize = config.IdealLinearStepSize;
      StepAngle = config.StepAngle;
      CalculateMicroStepAngle(config.StepMode.ToInternal());
    }
  }

  private void CalculateMicroStepAngle(StepMode stepMode)
  {
    if(StepAngle is null)
      return;

    double angle = (double)StepAngle;

    switch(stepMode)
    {
      case StepMode.Step1_2:
        MicroStepAngle = angle / 2.0;
        return;

      case StepMode.Step1_4:
        MicroStepAngle = angle / 4.0;
        break;

      case StepMode.Step1_8:
        MicroStepAngle = angle / 8.0;
        return;

      case StepMode.Step1_16:
        MicroStepAngle = angle / 16.0;
        return;

      case StepMode.Step1_32:
        MicroStepAngle = angle / 32;
        return;

      case StepMode.Step1_2_100:
        //Not supported
        break;

      case StepMode.Step1_64:
        MicroStepAngle = angle / 64;
        break;

      case StepMode.Step1_128:
        MicroStepAngle = angle / 128;
        break;

      case StepMode.Step1_256:
        MicroStepAngle = angle / 256;
        break;
    }
  }

  public async Task Start()
  {
    await StopStateUpdater();
    StartStateUpdater(TimeSpan.FromMilliseconds(250));
  }

  public Task Stop()
  {
    return StopStateUpdater();
  }

  public IObservable<Messaging.TicState> StateStream { get; }

  public uint UserStepSize { get; set; } = 1;

  private void StartStateUpdater(TimeSpan interval)
  {
    _stateUpdaterCancellation = new CancellationTokenSource();
    _stateUpdater = Task.Factory.StartNew(async _ =>
    {
      Thread.CurrentThread.Name = "Stepper Controller State Updater Thread";
      while(!_stateUpdaterCancellation.IsCancellationRequested)
      {
        await UpdateState();
        await Task.Delay(interval);
      }
    }, _stateUpdaterCancellation.Token, TaskCreationOptions.LongRunning);
  }

  private Task StopStateUpdater()
  {
    _stateUpdaterCancellation.Cancel();
    return _stateUpdater;
  }

  private async Task UpdateState()
  {
    var state = await GetStateFromDevice();
    _stateSubject.OnNext(state);
  }

  public override async Task<AresStruct> GetState()
  {
    var latestState = await _stateSubject.Take(1);

    return AresStateBuilder.Create()
      .Add("Current Position", latestState.CurrentPosition)
      .Add("Current Limit", latestState.CurrentLimit)
      .Add("Custom Step Size", latestState.CustomStepSize)
      .Add("Starting Speed", latestState.StartingSpeed)
      .Add("Max Acceleration", latestState.MaxAcceleration)
      .Add("Max Deceleration", latestState.MaxDeceleration)
      .Add("Max Speed", latestState.MaxSpeed)
      .Add("Step Mode", latestState.StepMode.ToString())
      .Add("Target Position", latestState.TargetPosition)
      .Build();
  }

  private async Task<Messaging.TicState> GetStateFromDevice()
  {
    var maxAccel = await GetMaxAcceleration();
    var maxDecel = await GetMaxDeceleration();
    var maxSpeed = await GetMaxSpeed();
    var limit = await GetCurrentLimit();
    var startingSpeed = await GetStartingSpeed();
    var stepMode = await GetStepMode();
    var currentPosition = await GetCurrentPosition();
    var targetPosition = await GetTargetPosition();
    var miscFlags = await GetMiscFlags();
    var errorStatus = await GetErrorStatus();
    var errorsOccurred = await GetErrorsOccurred();
    var state = new Messaging.TicState
    {
      MaxAcceleration = maxAccel.Acceleration,
      MaxDeceleration = maxDecel.Deceleration,
      MaxSpeed = maxSpeed.Speed,
      CurrentLimit = limit.Limit,
      StartingSpeed = startingSpeed.Speed,
      CustomStepSize = UserStepSize,
      StepMode = stepMode.ToProto(),
      CurrentPosition = currentPosition.Position,
      TargetPosition = targetPosition.Position,
      MiscFlags = miscFlags.ToProto(),
      ErrorStatus = errorStatus.ToProto(),
      ErrorsOccurred = errorsOccurred.ToProto(),
      Valid = true
    };

    return state;
  }

  public async Task NextStep(TimeSpan? timeout)
  {
    var state = await StateStream.Take(1);
    if(state is null)
      return;

    var currentPosition = state.CurrentPosition;
    long targetPosition;

    if(SmartStepCalculation)
    {
      CalculateCurrentRadius();
      var angularDisplacement = 180 * IdealLinearStepSize / (Math.PI * CurrentSpoolRadius);
      var numberOfSteps = angularDisplacement / MicroStepAngle;
      if(numberOfSteps is null)
        return;

      targetPosition = (long)(currentPosition + numberOfSteps);
      TotalSpoolDisplacementInMicrosteps += (int)numberOfSteps;
    }

    else
      targetPosition = currentPosition + UserStepSize;

    await SetTargetPosition((int)targetPosition);
  }

  public async Task HalfStep(TimeSpan? timeout)
  {
    var state = await StateStream.Take(1);
    if(state is null)
      return;

    var currentPosition = state.CurrentPosition;
    long targetPosition;

    if(SmartStepCalculation)
    {
      CalculateCurrentRadius();
      var angularDisplacement = 180 * IdealLinearStepSize / (Math.PI * CurrentSpoolRadius);
      var numberOfSteps = angularDisplacement / MicroStepAngle;
      if(numberOfSteps is null)
        return;

      numberOfSteps /= 2;
      targetPosition = (long)(currentPosition + numberOfSteps);
      TotalSpoolDisplacementInMicrosteps += (int)numberOfSteps;
    }

    else
      targetPosition = currentPosition + (UserStepSize / 2);

    await SetTargetPosition((int)targetPosition);
  }

  private void CalculateCurrentRadius()
  {
    if(InitialSpoolRadius is not null && FilterPaperThickness is not null)
    {
      //Convert our displacement to degrees from microsteps to perform our calculations
      var displacementInDegrees = TotalSpoolDisplacementInMicrosteps * MicroStepAngle;
      CurrentSpoolRadius = InitialSpoolRadius + (FilterPaperThickness * Math.Floor(displacementInDegrees / 360.0)) ?? 0.0;
    }
  }

  public async Task PreviousStep(TimeSpan? timeout)
  {
    var state = await StateStream.Take(1);
    if(state is null)
      return;

    var currentPosition = state.CurrentPosition;
    long targetPosition;

    if(SmartStepCalculation)
    {
      CalculateCurrentRadius();
      var angularDisplacement = 180 * IdealLinearStepSize / (Math.PI * CurrentSpoolRadius);
      var numberOfSteps = angularDisplacement / MicroStepAngle;
      if(numberOfSteps is null)
        return;

      targetPosition = (long)(currentPosition - numberOfSteps);
      TotalSpoolDisplacementInMicrosteps += (int)numberOfSteps;
    }

    else
      targetPosition = currentPosition - UserStepSize;

    await SetTargetPosition((int)targetPosition);
    //await WaitForTargetPosition(timeout ?? TimeSpan.FromSeconds(10));
  }

  public async Task WaitForTargetPosition(TimeSpan timeout)
  {
    var startTime = DateTime.UtcNow;
    var state = await StateStream.Take(1);
    var targetPosition = state.TargetPosition;
    while(DateTime.UtcNow - startTime < timeout)
    {
      var currentPosition = await GetCurrentPosition();
      if(currentPosition.Position == targetPosition)
        return;
    }

    throw new TimeoutException($"Stepper did not achieve target position within {timeout}");
  }

  public async ValueTask DisposeAsync()
  {
    _stateUpdaterCancellation.Cancel();
    await _stateUpdater;
    _stateSubject.OnCompleted();
  }

  public double? InitialSpoolRadius { get; set; }
  public double? FilterPaperThickness { get; set; }
  public double? IdealLinearStepSize { get; set; }
  public bool SmartStepCalculation { get; set; }
  public double CurrentSpoolRadius { get; set; }
  public double TotalSpoolDisplacementInMicrosteps { get; set; } = 0;
  public double? StepAngle { get; set; }
  public double MicroStepAngle { get; set; }

}
