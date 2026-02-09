using Ares.Datamodel;
using ReactiveUI.SourceGenerators;
using UI.Features.Devices.Remote;
using UI.Features.Devices.Shared;

namespace UI.Backend.ViewModels.Devices.Remote;

public partial class RemoteDeviceUnitViewModel : DeviceUnitControlViewModel, IAsyncDisposable
{
  private readonly IAresDeviceAdapter _deviceAdapter;
  private IDisposable? _stateListener;
  private IDisposable? _statusListener;

  public RemoteDeviceUnitViewModel(IAresDeviceAdapter deviceAdapter) : base(deviceAdapter.Id, deviceAdapter.Name)
  {
    _deviceAdapter = deviceAdapter;
    StartStateUpdater();

    ViewType = typeof(RemoteDeviceUnitView);
  }

  private void StartStateUpdater()
  {
    _stateListener = _deviceAdapter.StateStream.Subscribe(s => DeviceState = s);
    _statusListener = _deviceAdapter.ConnectionStatusStream.Subscribe(s => ConnectionStatus = s);
  }

  [Reactive]
  public partial AresStruct? DeviceState { get; private set; }

  [Reactive]
  public partial ConnectionStatus ConnectionStatus { get; private set; }

  public string DeviceName => _deviceAdapter.Name;

  public ValueTask DisposeAsync()
  {
    _stateListener?.Dispose();
    _statusListener?.Dispose();
    GC.SuppressFinalize(this);
    return ValueTask.CompletedTask;
  }
}
