using Ares.Datamodel;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Ares.Device.Tests.Device;

public class TestDevice : AresDevice
{
  private readonly BehaviorSubject<AresStruct> _stateSubject = new(new AresStruct());

  public TestDevice() : base("Test Device", "TestDevice")
  {
  }

  public override Task EnterSafeMode(CancellationToken ct)
    => Task.CompletedTask;

  public override Task<bool> Activate(CancellationToken ct)
    => Task.FromResult(true);

  public override Task<AresStruct> GetState()
    => Task.FromResult(new AresStruct());

  public override IObservable<AresStruct> StateStream => _stateSubject.AsObservable();
}
