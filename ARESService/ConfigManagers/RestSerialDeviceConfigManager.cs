using Ares.Core;
using Ares.Core.Device;
using Microsoft.EntityFrameworkCore;
using RestSerialDevice;
using RestSerialDevice.Config;

namespace AresService.ConfigManagers;

public class RestSerialDeviceConfigManager : DeviceConfigManagerBase<RestSerialConfig, ISerialRestDevice>
{
  public RestSerialDeviceConfigManager(IDbContextFactory<CoreDatabaseContext> dbContextFactory) : base(dbContextFactory)
  {

  }
}
