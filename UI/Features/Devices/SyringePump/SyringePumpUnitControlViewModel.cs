using Ares.SyringePump.Ne1000.Messaging;
using DynamicData.Binding;
using ReactiveUI.SourceGenerators;
using UI.Features.Devices.Shared;

namespace UI.Features.Devices.SyringePump;

public partial class SyringePumpUnitControlViewModel : DeviceUnitControlViewModel, IAsyncDisposable
{
  private SyringePumpRpc.SyringePumpRpcClient _syringePumpClient;
  private DeviceRequest _deviceRequest;
  private CancellationTokenSource _stateCts = new();
  private Task _stateListener = Task.CompletedTask;
  public SyringePumpUnitControlViewModel(string id, string syringePumpName, SyringePumpRpc.SyringePumpRpcClient syringePumpClient) : base(id, syringePumpName)
  {
    _syringePumpClient = syringePumpClient;
    _deviceRequest = new DeviceRequest { DeviceId = DeviceId };
    CurrentState = new StateResponse();
    Status = "Not Connected";
    Initialize();
    this.WhenValueChanged(vm => vm.CurrentState).Subscribe(_ => UpdateStatus());
    ViewType = typeof(SyringePumpUnitControl);
    DefaultWidth = 35;
  }

  private void UpdateStatus()
  {
    Status = CurrentState.Status switch
    {
      StatusPrompt.UndefinedStatusPrompt => "Error",
      StatusPrompt.PromptI => $"Infusing {CurrentState.Phase.Rate:F} mL/minute",
      StatusPrompt.PromptW => $"Withdrawing {CurrentState.Phase.Rate:F} mL/minute",
      StatusPrompt.PromptS => "Stopped",
      StatusPrompt.PromptP => "Paused",
      StatusPrompt.PromptT => "Timed Pause Phase",
      StatusPrompt.PromptU => "User Trigger",
      StatusPrompt.PromptX => "Purging",
      _ => throw new ArgumentOutOfRangeException()
    };
  }

  private void Initialize()
  {
    CurrentState = _syringePumpClient.GetCurrentState(_deviceRequest);
    _stateListener = Task.Run(async () =>
    {
      Thread.CurrentThread.Name = "Syringe Pump UI State Listener Thread";
      while(!_stateCts.IsCancellationRequested)
      {
        CurrentState = await _syringePumpClient.GetCurrentStateAsync(_deviceRequest);
        UpdateValues();
        await Task.Delay(TimeSpan.FromSeconds(2));
      }
    }, _stateCts.Token);
  }

  public async Task SetDiameter()
  {
    var request = new SetDiameterMmRequest { DeviceRequest = _deviceRequest, DiameterMm = TargetDiameterMm };
    await _syringePumpClient.SetDiameterAsync(request);
  }

  public async Task SetAddress()
  {
    var request = new SetAddressRequest() { DeviceRequest = _deviceRequest, Address = TargetAddress };
    await _syringePumpClient.SetAddressAsync(request);
  }

  public async Task SetPhaseFunctionRate()
  {
    var request = new SetProgramFunctionRateMmpmRequest
    { DeviceRequest = _deviceRequest, RateMmpm = TargetRateMmpm };
    await _syringePumpClient.SetProgramFunctionRateAsync(request);
  }

  public async Task SetPhaseFunctionDirection()
  {
    var request = new SetProgramFunctionPumpingDirectionRequest { DeviceRequest = _deviceRequest, Direction = TargetDirection };
    await _syringePumpClient.SetProgramFunctionPumpingDirectionAsync(request);
  }

  public async Task SetPhase()
  {
    var request = new SetPhaseNumberRequest { DeviceRequest = _deviceRequest, Phase = TargetPhase };
    await _syringePumpClient.SetPhaseAsync(request);
  }

  public async Task SetPhaseFunction()
  {
    var request = new SetPhaseFunctionRequest { DeviceRequest = _deviceRequest, Function = TargetFunction };
    await _syringePumpClient.SetPhaseFunctionAsync(request);
  }

  public async Task ClearVolumeDispensed()
  {
    var request = new ClearVolumeDispensedRequest { DeviceRequest = _deviceRequest, Direction = CurrentState.Phase.Direction };
    await _syringePumpClient.ClearVolumeDispensedAsync(request);
  }

  public async Task Purge()
  {
    await _syringePumpClient.PurgePumpAsync(_deviceRequest);
  }

  public async Task Start()
  {
    await _syringePumpClient.StartPumpingProgramAsync(_deviceRequest);
  }

  public async Task Stop()
  {
    await _syringePumpClient.StopPumpingProgramAsync(_deviceRequest);
  }

  public async ValueTask DisposeAsync()
  {
    _stateCts.Cancel();
    await _stateListener;
    _stateCts.Dispose();

    GC.SuppressFinalize(this);
  }

  private void UpdateValues()
  {
    if(CurrentState.Phase is null)
      return;

    TargetAddress = CurrentState.Address;
    ActiveDirection = CurrentState.Phase.Direction;
  }

  [Reactive]
  public partial int TargetAddress { get; set; }
  [Reactive]
  public partial float TargetDiameterMm { get; set; }
  [Reactive]
  public partial float TargetVolumeMl { get; set; }
  [Reactive]
  public partial float TargetRateMmpm { get; set; }
  [Reactive]
  public partial int TargetPhase { get; set; }
  [Reactive]
  public partial Commands TargetFunction { get; set; }
  [Reactive]
  public partial Direction TargetDirection { get; set; }
  [Reactive]
  public partial Direction ActiveDirection { get; set; }
  [Reactive]
  public partial StateResponse CurrentState { get; private set; }
  [Reactive]
  public partial string Status { get; private set; }
  [Reactive]
  public partial VolumeUnit VolumeUnit { get; set; }
}

