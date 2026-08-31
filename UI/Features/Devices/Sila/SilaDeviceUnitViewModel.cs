using Ares.Core.Device.Sila;
using Ares.Datamodel;
using Ares.Toolkit.Device.UI;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace UI.Features.Devices.Sila;

public partial class SilaDeviceUnitViewModel : ReactiveObject, IDeviceUnitControlViewModel, IAsyncDisposable
{
  private readonly SilaDevice _device;
  private IDisposable? _stateListener;

  public SilaDeviceUnitViewModel(SilaDevice device)
  {
    _device = device;
    DeviceId = device.UniqueId;
    StartStateUpdater();

    ViewType = typeof(SilaDeviceUnitView);
    DefaultWidth = 20;
  }

  private void StartStateUpdater()
  {
    _stateListener = _device.StateStream.Subscribe(s => DeviceState = s);
  }

  [Reactive]
  public partial AresStruct? DeviceState { get; set; }

  public string DeviceName => _device.Name;

  public string DeviceId { get; }

  public string Address => _device.Address;

  public int DefaultWidth { get; set; }
  public Type? ViewType { get; set; }

  public ValueTask DisposeAsync()
  {
    _stateListener?.Dispose();
    GC.SuppressFinalize(this);
    return ValueTask.CompletedTask;
  }
}
