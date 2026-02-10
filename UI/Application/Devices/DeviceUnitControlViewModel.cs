using ReactiveUI;

namespace UI.Application.Devices;

public abstract class DeviceUnitControlViewModel : ReactiveObject
{
  protected DeviceUnitControlViewModel(string deviceId, string deviceName)
  {
    DeviceName = deviceName;
    DeviceId = deviceId;
  }

  public string DeviceName { get; }

  public string DeviceId { get; }

  public int DefaultWidth { get; set; } = 20;

  public Type? ViewType { get; set; }
}
