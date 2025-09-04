using ReactiveUI;

namespace UI.Backend.ViewModels;

public abstract class UsbDeviceUnitViewModel : ReactiveObject
{
  protected UsbDeviceUnitViewModel(string deviceId, string deviceName)
  {
    DeviceName = deviceName;
    DeviceId = deviceId;
  }

  public string DeviceName { get; }

  public string DeviceId { get; }
}
