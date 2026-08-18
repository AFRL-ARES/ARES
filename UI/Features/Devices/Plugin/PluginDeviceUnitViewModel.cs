using Ares.Datamodel;
using Ares.Device;
using Ares.Toolkit.Device.UI;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace UI.Features.Devices.Plugin;

/// <summary>
/// A default view model that can be used when we fail to load a device plugins view model implementation.
/// </summary>
public class PluginDeviceUnitViewModel : ReactiveObject, IDeviceUnitControlViewModel, IAsyncDisposable
{
  private readonly IAresDevice _device;
  private IDisposable? _stateListener;

  public PluginDeviceUnitViewModel(IAresDevice device)
  {
    _device = device;
    DeviceId = device.UniqueId;
    StartStateUpdater();
    ViewType = typeof(PluginDeviceUnitView);
  }

  private void StartStateUpdater() =>
    _stateListener = _device.StateStream.Subscribe(s => DeviceState = s);

  [Reactive]
  public AresStruct? DeviceState { get; private set; }

  public string DeviceName => _device.Name;

  public string DeviceId { get; }
  public int DefaultWidth { get; set; }
  public Type? ViewType { get; set; }

  public ValueTask DisposeAsync()
  {
    _stateListener?.Dispose();
    GC.SuppressFinalize(this);
    return ValueTask.CompletedTask;
  }
}
