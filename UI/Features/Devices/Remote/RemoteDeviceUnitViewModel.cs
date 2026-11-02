using Ares.Datamodel;
using Ares.Datamodel.Device;
using Ares.Toolkit.Device.UI;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using UI.Application.Devices;

namespace UI.Features.Devices.Remote;

public partial class RemoteDeviceUnitViewModel : ReactiveObject, IDeviceUnitControlViewModel, IAsyncDisposable
{
  private readonly IAresDeviceAdapter _deviceAdapter;
  private IDisposable? _stateListener;
  private IDisposable? _statusListener;

  public RemoteDeviceUnitViewModel(IAresDeviceAdapter deviceAdapter)
  {
    DeviceId = deviceAdapter.Id;
    _deviceAdapter = deviceAdapter;
    StartStateUpdater();

    ViewType = typeof(RemoteDeviceUnitView);
    DefaultWidth = 20;
  }

  private void StartStateUpdater()
  {
    _stateListener = _deviceAdapter.StateStream.Subscribe(s => DeviceState = s);
    _statusListener = _deviceAdapter.ConnectionStatusStream.Subscribe(s =>
    {
      ConnectionStatus = s;
      if(s == ConnectionStatus.ConnectedToDevice)
        _ = RefreshCommandsAsync();
    });
    _ = RefreshCommandsAsync();
  }

  private async Task RefreshCommandsAsync()
  {
    try
    {
      DeviceCommands = (await _deviceAdapter.GetCommandDescriptorsAsync()).ToArray();
    }
    catch(Exception)
    {
      // Keep the last known command set available while the device reconnects.
    }
  }

  [Reactive]
  public partial AresStruct? DeviceState { get; set; }

  [Reactive]
  public partial ConnectionStatus ConnectionStatus { get; set; }

  [Reactive]
  public partial DeviceCommandDescriptor[] DeviceCommands { get; private set; } = [];

  public string DeviceName => _deviceAdapter.Name;
  public IAresDeviceAdapter DeviceAdapter => _deviceAdapter;

  public string DeviceId { get; }

  public int DefaultWidth { get; set; }
  public Type? ViewType { get; set; }

  public ValueTask DisposeAsync()
  {
    _stateListener?.Dispose();
    _statusListener?.Dispose();
    GC.SuppressFinalize(this);
    return ValueTask.CompletedTask;
  }
}
