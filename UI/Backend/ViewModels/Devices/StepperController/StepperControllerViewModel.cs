using ReactiveUI.Fody.Helpers;
using TicStepperController.Messaging;

namespace UI.Backend.ViewModels.StepperController;

public class StepperControllerViewModel : SerialDeviceUnitViewModel, IAsyncDisposable
{
  private readonly StepperControllerRpc.StepperControllerRpcClient _client;
  private readonly CancellationTokenSource _stateUpdateTokenSource = new();
  private Task _stateListener = Task.CompletedTask;

  public StepperControllerViewModel(string name, StepperControllerRpc.StepperControllerRpcClient client) : base(name)
  {
    _client = client;
    StartStateUpdater();
  }

  #region Properties

  [Reactive]
  public uint MaxAcceleration { get; private set; }
  [Reactive]
  public uint MaxDeceleration { get; private set; }
  [Reactive]
  public uint MaxSpeed { get; private set; }
  [Reactive]
  public uint StartingSpeed { get; private set; }
  [Reactive]
  public StepMode StepMode { get; private set; }
  [Reactive]
  public int CurrentPosition { get; private set; }
  [Reactive]
  public int TargetPosition { get; private set; }
  [Reactive]
  public MiscFlags? MiscFlags { get; private set; }
  [Reactive]
  public ErrorStatus? ErrorStatus { get; private set; }
  [Reactive]
  public ErrorsOccurred? ErrorsOccurred { get; private set; }

  #endregion

  #region Actions

  public Task ExitSafeStart()
  {
    return _client.ExitSafeStartAsync(new TicRequest { TicName = DeviceName }).ResponseAsync;
  }

  public Task EnterSafeStart()
  {
    return _client.EnterSafeStartAsync(new TicRequest { TicName = DeviceName }).ResponseAsync;
  }

  public Task HaltAndHold()
  {
    return _client.HaltAndHoldAsync(new TicRequest { TicName = DeviceName }).ResponseAsync;
  }

  public Task NextStep()
  {
    return _client.NextStepAsync(new TicRequest { TicName = DeviceName }).ResponseAsync;
  }

  public Task PreviousStep()
  {
    return _client.PreviousStepAsync(new TicRequest { TicName = DeviceName }).ResponseAsync;
  }

  public Task HalfStep()
  {
    return _client.HalfStepAsync(new TicRequest { TicName = DeviceName }).ResponseAsync;
  }

  public Task SetTargetPosition(int position)
  {
    return _client.SetTargetPositionAsync(new PositionCommand { TicName = DeviceName, Position = position }).ResponseAsync;
  }

  #endregion

  private void StartStateUpdater()
  {
    _stateListener = Task.Factory.StartNew(async _ =>
    {
      Thread.CurrentThread.Name = "Stepper Controller State Listener View Model Thread";
      var cancelled = false;
      lock (_stateUpdateTokenSource) { cancelled = _stateUpdateTokenSource.IsCancellationRequested; }
      while (!cancelled)
      {
        await UpdateState();
        await Task.Delay(TimeSpan.FromMilliseconds(500));
        lock (_stateUpdateTokenSource) { cancelled = _stateUpdateTokenSource.IsCancellationRequested; }
      }
    },
      _stateUpdateTokenSource.Token,
      TaskCreationOptions.LongRunning);
  }

  private void StopStateUpdater()
  {
    _stateUpdateTokenSource.Cancel();
  }

  private async Task UpdateState()
  {
    var response = await _client.GetStateAsync(new TicRequest { TicName = DeviceName });
    if (!response.Valid)
      return;

    MaxAcceleration = response.MaxAcceleration;
    MaxDeceleration = response.MaxDeceleration;
    MaxSpeed = response.MaxSpeed;
    StartingSpeed = response.StartingSpeed;
    StepMode = response.StepMode;
    CurrentPosition = response.CurrentPosition;
    TargetPosition = response.TargetPosition;
    MiscFlags = response.MiscFlags;
    ErrorStatus = response.ErrorStatus;
    ErrorsOccurred = response.ErrorsOccurred;
  }

  public async ValueTask DisposeAsync()
  {
    StopStateUpdater();
    await _stateListener;
    _stateListener.Dispose();
    _stateUpdateTokenSource.Dispose();
    GC.SuppressFinalize(this);
  }
}
