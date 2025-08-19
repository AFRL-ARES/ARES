using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ares.Core.Analyzing;
using Ares.Datamodel;
using Ares.Datamodel.Analyzing;
using Ares.Datamodel.Device;

namespace Ares.Core.Device.Remote;
internal interface IDeviceCache
{
  Task CacheDeviceInfo(RemoteDevice device);
  Task CacheDeviceSettings(RemoteDevice device);
  Task<DeviceInfo?> GetCachedDeviceInfo(string deviceId);
  Task<AresStruct?> GetCachedDeviceSettings(string deviceId);
}
