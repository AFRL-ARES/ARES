using Ares.Datamodel;
using Ares.Device;

namespace Ares.Core.Device.Managers;

public interface IDeviceManager
{
  Task<IAresDevice> Create(string name, bool isSimulated, AresStruct settings);
  Task<IAresDevice> Load(string deviceId, DeviceConfig config);
  Task<IAresDevice[]> Load(IEnumerable<DeviceConfig> configs);
  Task<IAresDevice> Update(string deviceId, DeviceConfig config);
  Task Remove(string deviceId);
}
