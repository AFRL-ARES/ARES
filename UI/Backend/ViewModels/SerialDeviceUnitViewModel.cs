using ReactiveUI;

namespace UI.Backend.ViewModels;

public abstract class SerialDeviceUnitViewModel : ReactiveObject
{
  protected SerialDeviceUnitViewModel(string deviceName)
  {
    DeviceName = deviceName;
  }

  public string DeviceName { get; }
}
