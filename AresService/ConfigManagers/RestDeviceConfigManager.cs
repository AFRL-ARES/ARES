using Ares.Core;
using Ares.Core.Device;
using Microsoft.EntityFrameworkCore;
using RestDevice;
using RestDevice.Config;

namespace AresService.ConfigManagers;

public class RestDeviceConfigManager : DeviceConfigManagerBase<RestDeviceConfig, IRestDevice>
{
  public RestDeviceConfigManager(IDbContextFactory<CoreDatabaseContext> dbContextFactory) : base(dbContextFactory)
  {

  }
}
