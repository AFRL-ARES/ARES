using Ares.Device;
using Ares.Messaging.Device;

namespace CoreDevice;

public class AresCoreDevice : IAresDevice
{
  public string Name => "ARES";

  public DeviceStatus Status { get; } = new DeviceStatus { DeviceState = DeviceState.Active };

  public Task<bool> Activate()
  {
    return Task.FromResult(true);
  }

  public Task Sleep(TimeSpan timeSpan)
  {
    return Task.Delay(timeSpan);
  }
}
