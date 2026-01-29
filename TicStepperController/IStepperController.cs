using Ares.Device.Serial;
using TicStepperController.Commands.Enums;
using TicStepperController.Commands.Responses;
using TicStepperController.Config;

namespace TicStepperController;
public interface IStepperController : ISerialDevice<IStepperControllerConnection>, IAsyncDisposable
{
  Task<OperationState> GetOperationState();
  Task<CurrentPosition> GetCurrentPosition();
  Task<ErrorsOccurred> GetErrorsOccurred();
  Task<ErrorStatus> GetErrorStatus();
  Task<MaxAcceleration> GetMaxAcceleration();
  Task<MaxDeceleration> GetMaxDeceleration();
  Task<CurrentLimit> GetCurrentLimit();
  Task<MaxSpeed> GetMaxSpeed();
  Task<MiscFlags> GetMiscFlags();
  Task<StartingSpeed> GetStartingSpeed();
  Task<StepMode> GetStepMode();
  Task<TargetPosition> GetTargetPosition();

  /// <summary>
  /// Hardware reset, sets values back to the ones stored within the Tic itself
  /// </summary>
  /// <returns></returns>
  Task Reset();

  /// <summary>
  /// Initializes the device with the values stored within the config
  /// </summary>
  /// <returns></returns>
  Task Init(StepperControllerConfig config);
  Task Energize();
  Task DeEnergize();
  Task EnterSafeStart();
  Task ExitSafeStart();
  Task HaltAndHold();
  Task ResetCommandTimeout();

  Task SetMaxAcceleration(uint acceleration);
  Task SetMaxDeceleration(uint deceleration);
  Task SetCurrentLimit(uint limit);
  Task SetMaxSpeed(uint speed);
  Task SetStartingSpeed(uint speed);
  Task SetStepMode(StepMode stepMode);
  Task SetTargetPosition(int position);

  Task WaitForTargetPosition(TimeSpan timeout);
  Task HaltAndSetPosition(int position);

  Task NextStep(TimeSpan? timeout = null);
  Task PreviousStep(TimeSpan? timeout = null);
  Task HalfStep(TimeSpan? timeout = null);

  IObservable<Messaging.TicState> InternalStateStream { get; }
  uint UserStepSize { get; set; }
  Task Start();
  Task Stop();
  public double? InitialSpoolRadius { get; }
  public double? FilterPaperThickness { get; }
  public double? IdealLinearStepSize { get; }
  public bool SmartStepCalculation { get; }
}
