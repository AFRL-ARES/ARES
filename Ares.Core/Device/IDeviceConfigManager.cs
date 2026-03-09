using Ares.Datamodel.Device;

namespace Ares.Core.Device;

public interface IDeviceConfigManager
{
  /// <summary>
  /// Initializes the config manager class by adding all known device configs from the database to the local repo 
  /// </summary>
  /// <returns></returns>
  Task LoadConfigs();
  Task Add(DeviceConfig config); 
  Task Remove(string configId);
  Task Update(string configId, DeviceConfig config);
}
