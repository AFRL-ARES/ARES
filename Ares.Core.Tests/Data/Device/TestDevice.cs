using Ares.Device;

namespace Ares.Core.Tests.Data.Device;

public class TestDevice : AresDevice
{

  public TestDevice() : base("Test Device")
  {
  }

  public override Task<bool> Activate()
    => Task.FromResult(true);
}
