using Ares.Device;

namespace Ares.Core.CoreDevice;

public class AresCoreDevice : AresDevice
{
  public AresCoreDevice() : base("ARES", "ARES-CORE-DEVICE")
  {
  }

  public override Task<bool> Activate(CancellationToken ct)
  {
    return Task.FromResult(true);
  }

  public override Task EnterSafeMode(CancellationToken ct)
  {
    return Task.CompletedTask;
  }

  public Task Sleep(TimeSpan timeSpan)
  {
    return Task.Delay(timeSpan);
  }
}
