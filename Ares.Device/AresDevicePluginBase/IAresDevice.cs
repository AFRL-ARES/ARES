using System.Threading.Tasks;
using Ares.Datamodel.Device;

namespace Ares.Device;

public interface IAresDevice
{
  string Name { get; }
  DeviceStatus Status { get; }
  Task<bool> Activate();
  Task EnterSafeMode();
}
