using System.Reactive.Linq;
using System.Reactive.Subjects;
using Ares.Device.Serial;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
    if (logger is not null)
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

  protected override async Task<DeviceValidationResult> Validate()
  {
    try
    {
      var operationResponse = await GetOperationState();
      return new DeviceValidationResult(true);
    }
    catch (Exception e)
    {
      return new DeviceValidationResult(false, e.Message);
    }
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
    if (config.MaxAcceleration.HasValue)
      await SetMaxAcceleration(config.MaxAcceleration.Value);

    if (config.MaxDeceleration.HasValue)
      await SetMaxDeceleration(config.MaxDeceleration.Value);

    if (config.MaxSpeed.HasValue)
      await SetMaxSpeed(config.MaxSpeed.Value);

    if (config.StartingSpeed.HasValue)
      await SetStartingSpeed(config.StartingSpeed.Value);

    if (config.StepMode != Messaging.StepMode.Undefined)
      await SetStepMode(config.StepMode.ToInternal());

    UserStepSize = config.CustomStepSize ?? 1;
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
      while (!_stateUpdaterCancellation.IsCancellationRequested)
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

  private async Task<Messaging.TicState> GetStateFromDevice()
  {
    var maxAccel = await GetMaxAcceleration();
    var maxDecel = await GetMaxDeceleration();
    var maxSpeed = await GetMaxSpeed();
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
    if (state is null)
      return;

    var currentPos = state.CurrentPosition;
    var targetPos = currentPos + UserStepSize;
    await SetTargetPosition((int)targetPos);
    //await WaitForTargetPosition(timeout ?? TimeSpan.FromSeconds(10));
  }

  public async Task PreviousStep(TimeSpan? timeout)
  {
    var state = await StateStream.Take(1);
    if (state is null)
      return;

    var currentPos = state.CurrentPosition;
    var targetPos = currentPos - UserStepSize;
    await SetTargetPosition((int)targetPos);
    //await WaitForTargetPosition(timeout ?? TimeSpan.FromSeconds(10));
  }

  public async Task WaitForTargetPosition(TimeSpan timeout)
  {
    var startTime = DateTime.UtcNow;
    var state = await StateStream.Take(1);
    var targetPosition = state.TargetPosition;
    while (DateTime.UtcNow - startTime < timeout)
    {
      var currentPosition = await GetCurrentPosition();
      if (currentPosition.Position == targetPosition)
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
}
