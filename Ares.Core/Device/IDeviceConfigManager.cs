using Ares.Datamodel.Device;

namespace Ares.Core.Device;

public interface IDeviceConfigManager
{
  Task Add(string deviceId, string deviceName, DeviceConfig config); 
  Task Remove(string configId);
  Task Update(string configId, DeviceConfig config);
}
