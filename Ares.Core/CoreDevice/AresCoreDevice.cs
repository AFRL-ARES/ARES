using Ares.Datamodel.Device;
using Ares.Device;

namespace Ares.Core.CoreDevice;

public class AresCoreDevice : AresDevice
{
  public AresCoreDevice() : base("ARES")
  {
  }

  public override Task<bool> Activate()
  {
    return Task.FromResult(true);
  }

  public override Task EnterSafeMode()
  {
    return Task.CompletedTask;
  }

  public Task Sleep(TimeSpan timeSpan)
  {
    return Task.Delay(timeSpan);
  }
}
