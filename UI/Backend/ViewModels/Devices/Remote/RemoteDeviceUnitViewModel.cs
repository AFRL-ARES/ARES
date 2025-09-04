using Ares.Datamodel;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using UI.Backend.Devices;

namespace UI.Backend.ViewModels.Devices.Remote;

public class RemoteDeviceUnitViewModel : ReactiveObject, IAsyncDisposable
{
  private readonly CancellationTokenSource _stateUpdateTokenSource = new();
  private readonly IAresDeviceAdapter _deviceAdapter;
  private IDisposable? _stateListener;

  public RemoteDeviceUnitViewModel(IAresDeviceAdapter deviceAdapter)
  {
    _deviceAdapter = deviceAdapter;
    StartStateUpdater();
  }

  private void StartStateUpdater()
  {
    _stateListener = _deviceAdapter.StateStream.Subscribe(s => DeviceState = s);
  }

  [Reactive]
  public AresStruct? DeviceState { get; private set; }

  public string DeviceName => _deviceAdapter.Name;

  public ValueTask DisposeAsync()
  {
    _stateListener?.Dispose();
    _stateUpdateTokenSource.Dispose();
    GC.SuppressFinalize(this);
    return ValueTask.CompletedTask;
  }
}
