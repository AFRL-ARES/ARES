using Ares.Core.CustomCommands;
using Ares.Datamodel;
using Ares.Datamodel.Device;
using Ares.Device;
using System.Reactive.Linq;

namespace Ares.Core.CustomCommandDevice;

internal class AresCommandDevice : AresDevice
{
  private readonly CustomCommandPersistenceService _commandPersistenceService;

  public AresCommandDevice(CustomCommandPersistenceService commandPersistenceService) : base(new DeviceConnectionInfo { DeviceName = "Custom Command", DeviceId = "ARES-CUSTOM-COMMAND-DEVICE" })
  {
    _commandPersistenceService = commandPersistenceService;
  }

  public override IObservable<AresStruct> StateStream => Observable.Empty<AresStruct>();

  public override Task<bool> Activate(CancellationToken ct)
  {
    return Task.FromResult(true);
  }

  public override Task EnterSafeMode(CancellationToken ct)
  {
    throw new NotImplementedException();
  }

  public override Task<CommandResult> ExecuteCommand(string command, List<DeviceCommandArgument> arguments, CancellationToken token)
  {
    throw new NotImplementedException();
  }

  public override Task<AresStruct> GetSettings()
  {
    return Task.FromResult(new AresStruct());
  }

  public override Task<AresStruct> GetState()
  {
    throw new NotImplementedException();
  }

  public override Task UpdateSettings(AresStruct settings)
  {
    throw new NotImplementedException();
  }

  protected override async Task<List<DeviceCommandDescriptor>> BuildCommandDescriptorsAsync()
  {
    var commands = await _commandPersistenceService.GetSummariesAsync();
    var descriptors = commands.Select(cmd => new DeviceCommandDescriptor { Name = cmd.Name, Description = cmd.Description, OutputSchema = cmd.OutputSummary })
  }
}
