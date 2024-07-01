using Ares.SyringePump.Ne1000.Messaging;
using DynamicData.Binding;
using ReactiveUI.Fody.Helpers;

namespace UI.Backend.ViewModels.SyringePump;

public class SyringePumpUnitControlViewModel : SerialDeviceUnitViewModel, IAsyncDisposable
{
  private SyringePumpRpc.SyringePumpRpcClient _syringePumpClient;
  private DeviceRequest _deviceRequest;
  private CancellationTokenSource _stateCts = new();
  private Task _stateListener = Task.CompletedTask;
  public SyringePumpUnitControlViewModel(string syringePumpName, SyringePumpRpc.SyringePumpRpcClient syringePumpClient) : base(syringePumpName)
  {
    _syringePumpClient = syringePumpClient;
    _deviceRequest = new DeviceRequest { DeviceName = DeviceName };
    Initialize();
    this.WhenValueChanged(vm => vm.CurrentState).Subscribe(_ => UpdateStatus());
  }

  private void UpdateStatus()
  {
    Status = CurrentState.Status switch
    {
      StatusPrompt.UndefinedStatusPrompt => "Error",
      StatusPrompt.PromptI => $"Infusing {CurrentState.Phase.Volume:F} {CurrentState.VolumeUnits}",
      StatusPrompt.PromptW => $"Withdrawing {CurrentState.Phase.Volume:F} {CurrentState.VolumeUnits}",
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
    var getCurrentStateRequest = new GetCurrentStateRequest { DeviceRequest = _deviceRequest };
    CurrentState = _syringePumpClient.GetCurrentState(getCurrentStateRequest);
    _stateListener = Task.Run(async () =>
    {
      while (!_stateCts.IsCancellationRequested)
      {
        CurrentState = await _syringePumpClient.GetCurrentStateAsync(getCurrentStateRequest);
        await Task.Delay(TimeSpan.FromMilliseconds(250));
      }
    }, _stateCts.Token);
  }

  public void GetAddress()
  {
    var request = new GetAddressRequest { DeviceRequest = _deviceRequest };
    _syringePumpClient.GetAddress(request);
  }

  public Task QueryPhase()
  {
    var request = new QueryPhaseRequest { DeviceRequest = _deviceRequest };
    return _syringePumpClient.QueryPhaseAsync(request).ResponseAsync;
  }

  public Task QueryPhaseFunction()
  {
    var request = new QueryPhaseFunctionRequest { DeviceRequest = _deviceRequest };
    return _syringePumpClient.QueryPhaseFunctionAsync(request).ResponseAsync;
  }

  public Task SetAddress()
  {
    var request = new SetAddressRequest { Address = TargetAddress, DeviceRequest = _deviceRequest };
    return _syringePumpClient.SetAddressAsync(request).ResponseAsync;
  }

  public Task GetDiameter()
  {
    var request = new GetDiameterRequest { DeviceRequest = _deviceRequest };
    return _syringePumpClient.GetDiameterAsync(request).ResponseAsync;
  }

  public Task SetDiameter()
  {
    var request = new SetDiameterMmRequest { DeviceRequest = _deviceRequest, DiameterMm = TargetDiameterMm };
    return _syringePumpClient.SetDiameterAsync(request).ResponseAsync;
  }

  public Task GetPhaseFunctionRate()
  {
    var request = new GetProgramFunctionRateRequest { DeviceRequest = _deviceRequest };
    return _syringePumpClient.GetProgramFunctionRateAsync(request).ResponseAsync;
  }

  public Task SetPhaseFunctionRate()
  {
    var request = new SetProgramFunctionRateMmpmRequest
    { DeviceRequest = _deviceRequest, RateMmpm = TargetRateMmpm };
    return _syringePumpClient.SetProgramFunctionRateAsync(request).ResponseAsync;
  }
  public Task GetPhaseFunctionVolume()
  {
    var request = new GetProgramFunctionVolumeToBeDispensedRequest { DeviceRequest = _deviceRequest };
    return _syringePumpClient.GetProgramFunctionVolumeToBeDispensedAsync(request).ResponseAsync;
  }
  public Task SetPhaseFunctionVolume()
  {
    var request = new SetProgramFunctionVolumeToBeDispensedRequest { DeviceRequest = _deviceRequest, VolumeMl = TargetVolumeMl };
    return _syringePumpClient.SetProgramFunctionVolumeToBeDispensedAsync(request).ResponseAsync;
  }

  public Task GetPhaseFunctionDirection()
  {
    var request = new GetProgramFunctionPumpingDirectionRequest { DeviceRequest = _deviceRequest };
    return _syringePumpClient.GetProgramFunctionPumpingDirectionAsync(request).ResponseAsync;
  }
  public Task SetPhaseFunctionDirection()
  {
    var request = new SetProgramFunctionPumpingDirectionRequest { DeviceRequest = _deviceRequest, Direction = TargetDirection };
    return _syringePumpClient.SetProgramFunctionPumpingDirectionAsync(request).ResponseAsync;
  }

  public Task SetPhase()
  {
    var request = new SetPhaseNumberRequest { DeviceRequest = _deviceRequest, Phase = TargetPhase };
    return _syringePumpClient.SetPhaseAsync(request).ResponseAsync;
  }

  public Task SetPhaseFunction()
  {
    var request = new SetPhaseFunctionRequest { DeviceRequest = _deviceRequest, Function = TargetFunction };
    return _syringePumpClient.SetPhaseFunctionAsync(request).ResponseAsync;
  }

  public Task ClearVolumeDispensed()
  {
    var request = new ClearVolumeDispensedRequest { DeviceRequest = _deviceRequest, Direction = CurrentState.Phase.Direction };
    return _syringePumpClient.ClearVolumeDispensedAsync(request).ResponseAsync;
  }

  public Task Purge()
  {
    var request = new PurgePumpRequest { DeviceRequest = _deviceRequest };
    return _syringePumpClient.PurgePumpAsync(request).ResponseAsync;
  }

  public Task Start()
  {
    var request = new StartPumpingProgramRequest { DeviceRequest = _deviceRequest };
    return _syringePumpClient.StartPumpingProgramAsync(request).ResponseAsync;
  }

  public Task Stop()
  {
    var request = new StopPumpingProgramRequest { DeviceRequest = _deviceRequest };
    return _syringePumpClient.StopPumpingProgramAsync(request).ResponseAsync;
  }

  public async ValueTask DisposeAsync()
  {
    _stateCts.Cancel();
    await _stateListener;
    _stateCts.Dispose();

    GC.SuppressFinalize(this);
  }

  [Reactive]
  public int TargetAddress { get; set; }
  [Reactive]
  public float TargetDiameterMm { get; set; }
  [Reactive]
  public float TargetVolumeMl { get; set; }
  [Reactive]
  public float TargetRateMmpm { get; set; }
  [Reactive]
  public int TargetPhase { get; set; }
  [Reactive]
  public Ares.SyringePump.Ne1000.Messaging.Commands TargetFunction { get; set; }
  [Reactive]
  public Ares.SyringePump.Ne1000.Messaging.Direction TargetDirection { get; set; }
  [Reactive]
  public StateResponse CurrentState { get; private set; }
  [Reactive]
  public string Status { get; private set; } = "Not Connected";
  [Reactive]
  public VolumeUnit VolumeUnit { get; set; }
}
