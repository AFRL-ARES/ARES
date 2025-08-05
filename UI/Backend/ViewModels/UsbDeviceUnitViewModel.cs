using ReactiveUI;

namespace UI.Backend.ViewModels;

public abstract class UsbDeviceUnitViewModel : ReactiveObject
{
  protected UsbDeviceUnitViewModel(string deviceName)
  {
    DeviceName = deviceName;
  }

  public string DeviceName { get; }
}
