using Ares.Datamodel;
using Ares.Datamodel.Device;

namespace Ares.Core.Device.Remote;
internal interface IDeviceCache
{
  Task CacheDeviceInfo(RemoteDevice device);
  Task CacheDeviceSettings(RemoteDevice device);
  Task<DeviceInfo?> GetCachedDeviceInfo(string deviceId);
  Task<AresStruct?> GetCachedDeviceSettings(string deviceId);
}
