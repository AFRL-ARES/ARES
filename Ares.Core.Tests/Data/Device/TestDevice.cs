using Ares.Datamodel;
using Ares.Device;

namespace Ares.Core.Tests.Data.Device;

public class TestDevice : AresDevice
{

  public TestDevice() : base("Test Device", "TestDevice")
  {
  }

  public override Task<bool> Activate(CancellationToken ct)
    => Task.FromResult(true);

  public override Task EnterSafeMode(CancellationToken ct) 
    => Task.CompletedTask;

  public override Task<AresStruct> GetState()
    => Task.FromResult(new AresStruct());
}
