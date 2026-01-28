using Ares.Datamodel;
using Ares.Datamodel.Device;
using Ares.Device;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Ares.Core.CoreDevice;

public class AresCoreDevice : AresDevice
{
  private readonly BehaviorSubject<AresStruct> _stateSubject = new(new AresStruct());

  public AresCoreDevice() : base("ARES", "ARES-CORE-DEVICE")
  {
    Status = new DeviceOperationalStatus()
    {
      OperationalState = OperationalState.Active
    };

    StateStream = _stateSubject.AsObservable();
  }

  public override Task<bool> Activate(CancellationToken ct)
  {
    return Task.FromResult(true);
  }

  public override Task EnterSafeMode(CancellationToken ct)
  {
    return Task.CompletedTask;
  }

  public override Task<AresStruct> GetState()
  {
    return Task.FromResult(new AresStruct());
  }

  public Task Sleep(TimeSpan timeSpan, CancellationToken ct)
  {
    return Task.Delay(timeSpan, ct);
  }

  public override IObservable<AresStruct> StateStream { get; }
}
