using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ares.Datamodel;
using Ares.Datamodel.Device;

namespace Ares.Device;

public interface IAresDevice : IDisposable
{
  string UniqueId { get; }
  string Name { get; }
  string Version { get; }
  string Type { get; }
  string Description { get; }
  string HardwareIdentity { get; }
  DeviceOperationalStatus Status { get; }
  IObservable<DeviceOperationalStatus> StatusObservable { get; }
  IObservable<AresStruct> StateStream { get; }
  Task<bool> Activate(CancellationToken ct = default);
  Task EnterSafeMode(CancellationToken ct = default);
  Task<AresStruct> GetState();
  IReadOnlyList<DeviceCommandDescriptor> CommandDescriptors { get; }
  AresDataSchema StateSchema { get; }
  AresDataSchema SettingSchema { get; }
  Task<CommandResult> ExecuteCommand(string command, AresStruct arguments, CancellationToken ct);
  Task UpdateSettings(AresStruct settings);
}
