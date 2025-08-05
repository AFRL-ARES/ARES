using Ares.Device;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AresService.DeviceManagers;

/// <summary>
/// </summary>
/// <typeparam name="TConfig">Config type used for loading the device</typeparam>
public interface IDeviceManager<in TConfig, TDevice> where TDevice : IAresDevice
{
  Task<TDevice> Load(TConfig config);
  Task<IEnumerable<TDevice>> Load(IEnumerable<TConfig> configs);
  Task<TDevice> Update(TConfig config);
  Task Remove(string deviceId);
  
  
}
