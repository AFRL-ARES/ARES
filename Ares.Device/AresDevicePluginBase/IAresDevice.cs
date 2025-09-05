using System;
using System.Threading;
using System.Threading.Tasks;
using Ares.Datamodel.Device;

namespace Ares.Device;

public interface IAresDevice : IDisposable
{
  string UniqueId { get; }
  string Name { get; }
  string Version { get; }
  string Type { get; }
  string Description { get; }
  DeviceOperationalStatus Status { get; }
  IObservable<DeviceOperationalStatus> StatusObservable { get; }
  Task<bool> Activate(CancellationToken ct = default);
  Task EnterSafeMode(CancellationToken ct = default);
}
