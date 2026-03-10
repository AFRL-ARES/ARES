using Ares.Datamodel;
using Ares.Datamodel.Device;
using Ares.Device;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Ares.Core.Tests.Data.Device;

public class TestDevice : AresDevice
{
  private readonly BehaviorSubject<AresStruct> _stateSubject = new(new AresStruct());

  public TestDevice() : base(new DeviceConnectionInfo { DeviceId = "TestDevice", DeviceName = "Test Device"})
  {
  }

  public TestDevice(string name, string id) : base(new DeviceConnectionInfo { DeviceName = name, DeviceId = id })
  {
  }

  public override Task<bool> Activate(CancellationToken ct)
    => Task.FromResult(true);

  public override Task EnterSafeMode(CancellationToken ct) 
    => Task.CompletedTask;

  public override Task<AresStruct> GetState()
    => Task.FromResult(new AresStruct());

  public override Task<CommandResult> ExecuteCommand(string command, List<DeviceCommandArgument> parameters, CancellationToken token)
  {
    return Task.FromResult(new CommandResult { Success = true });
  }

  public override Task UpdateSettings(AresStruct settings)
  {
    throw new NotImplementedException();
  }

  public override IObservable<AresStruct> StateStream => _stateSubject.AsObservable();
}
