using System.Threading.Tasks;
using Ares.Datamodel.Device;

namespace Ares.Device;

public interface IAresDevice
{
  string UniqueId { get; }
  string Name { get; }
  string Version { get; }
  string Type { get; }
  string Description { get; }
  DeviceOperationalStatus Status { get; }
  Task<bool> Activate();
  Task EnterSafeMode();
}
