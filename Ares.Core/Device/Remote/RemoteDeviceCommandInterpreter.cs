using Ares.Datamodel;
using Ares.Datamodel.Templates;
using Ares.Device;

namespace Ares.Core.Device.Remote;
internal class RemoteDeviceCommandInterpreter : DeviceCommandInterpreter<RemoteDevice, RemoteDeviceCommandEnum>
{
  public RemoteDeviceCommandInterpreter(RemoteDevice device) : base(device)
  {
  }

  protected override CommandMetadata[] CommandsToMetadatas()
  {
    var commands = CommandHelpers.ToCommandMetadata(RemoteDevice.CommandDescriptors, Device.UniqueId);
    return commands;
  }

  protected override async Task<CommandResult> ParseAndPerformDeviceAction(RemoteDeviceCommandEnum deviceCommandEnum, Parameter[] parameters, CommandMetadata metadata, CancellationToken cancellationToken)
  {
    var result = await RemoteDevice.ExecuteCommand(metadata.Name, CommandHelpers.ParametersToStruct(parameters), cancellationToken);

    return result;
  }

  private RemoteDevice RemoteDevice => Device;
}
