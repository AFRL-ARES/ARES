using Ares.Datamodel;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using UI.Backend.Devices;

namespace UI.Backend.ViewModels.Devices.Remote;

public class RemoteDeviceUnitViewModel : ReactiveObject, IAsyncDisposable
{
  private readonly IAresDeviceAdapter _deviceAdapter;
  private IDisposable? _stateListener;
  private IDisposable? _statusListener;

  public RemoteDeviceUnitViewModel(IAresDeviceAdapter deviceAdapter)
  {
    _deviceAdapter = deviceAdapter;
    StartStateUpdater();
  }

  private void StartStateUpdater()
  {
    _stateListener = _deviceAdapter.StateStream.Subscribe(s => DeviceState = s);
    _statusListener = _deviceAdapter.ConnectionStatusStream.Subscribe(s => ConnectionStatus = s);
  }

  [Reactive]
  public AresStruct? DeviceState { get; private set; }

  [Reactive]
  public ConnectionStatus ConnectionStatus { get; private set; }

  public string DeviceName => _deviceAdapter.Name;

  public ValueTask DisposeAsync()
  {
    _stateListener?.Dispose();
    _statusListener?.Dispose();
    GC.SuppressFinalize(this);
    return ValueTask.CompletedTask;
  }
}
