using Ares.Datamodel;
using Ares.Datamodel.Device;

namespace UI.Application.Devices;

public interface IAresDeviceAdapter
{
  string Id { get; }
  string Name { get; }
  string Description { get; }
  string Type { get; }
  string Version { get; }
  Task<bool> Activate();
  Task UpdateConnectionStatus();
  Task<IReadOnlyList<DeviceCommandDescriptor>> GetCommandDescriptorsAsync(CancellationToken cancellationToken = default);
  Task<CommandResult> ExecuteCommandAsync(string commandName,
    IReadOnlyCollection<DeviceCommandArgument> arguments,
    CancellationToken cancellationToken = default);
  AresStruct? State { get; }
  IObservable<AresStruct?> StateStream { get; }
  DeviceOperationalStatus OperationalStatus { get; }
  IObservable<ConnectionStatus> ConnectionStatusStream { get; }
}
