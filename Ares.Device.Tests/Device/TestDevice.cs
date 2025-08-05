namespace Ares.Device.Tests.Device;

public class TestDevice : AresDevice
{

  public TestDevice() : base("Test Device")
  {
  }

  public override Task EnterSafeMode()
    => Task.CompletedTask;

  public override Task<bool> Activate()
    => Task.FromResult(true);
}
