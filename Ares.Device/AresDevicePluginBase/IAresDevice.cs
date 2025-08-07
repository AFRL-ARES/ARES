using System.Threading.Tasks;
using Ares.DataModel.Device;

namespace Ares.Device;

public interface IAresDevice
{
  string Name { get; }
  DeviceStatus Status { get; }
  Task<bool> Activate();
}
