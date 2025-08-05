using AresService.DeviceManagers;
using Microsoft.EntityFrameworkCore;
using RestDevice;
using RestDevice.Config;

namespace AresService.DeviceDbLoaders;

public class RestDeviceDbLoader : DeviceDbLoaderBase<IRestDevice, RestDeviceConfig>
{
  public RestDeviceDbLoader(IDbContextFactory<AresDbContext> dbContextFactory, IDeviceManager<RestDeviceConfig, IRestDevice> deviceManager) : base(dbContextFactory, deviceManager)
  {
  }
}
