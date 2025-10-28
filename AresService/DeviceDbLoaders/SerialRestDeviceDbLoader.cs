using AresService.Data;
using AresService.DeviceManagers;
using Microsoft.EntityFrameworkCore;
using RestSerialDevice;
using RestSerialDevice.Config;

namespace AresService.DeviceDbLoaders;

public class SerialRestDeviceDbLoader : DeviceDbLoaderBase<ISerialRestDevice, RestSerialConfig>
{
  public SerialRestDeviceDbLoader(IDbContextFactory<AresDbContext> dbContextFactrory, IDeviceManager<RestSerialConfig, ISerialRestDevice> deviceManager) : base(dbContextFactrory, deviceManager)
  {
  }
}
