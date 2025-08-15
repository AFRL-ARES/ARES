using Ares.Device;
using Ares.Datamodel.Device;

namespace Ares.Core.CoreDevice;

public class AresCoreDevice : IAresDevice
{
  public AresCoreDevice()
  {
  }

  public string Name => "ARES";

  public DeviceOperationalStatus Status { get; } = new DeviceOperationalStatus { OperationalState = OperationalState.Active };

  public Task<bool> Activate()
  {
    return Task.FromResult(true);
  }

  public Task EnterSafeMode()
  {
    return Task.CompletedTask;
  }

  public Task Sleep(TimeSpan timeSpan)
  {
    return Task.Delay(timeSpan);
  }
}
