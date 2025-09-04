using ReactiveUI;

namespace UI.Backend.ViewModels;

public abstract class SerialDeviceUnitViewModel(string deviceId, string deviceName) : ReactiveObject
{
  public string DeviceName { get; } = deviceName;
  public string DeviceId { get; } = deviceId;
}
