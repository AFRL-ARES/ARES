using Ares.Core;
using Ares.Core.Device;
using Chiller.Config;
using LaserChiller;
using Microsoft.EntityFrameworkCore;

namespace AresService.ConfigManagers;

public class LaserChillerConfigManager : DeviceConfigManager<ChillerConfig, ILaserChiller>
{
  public LaserChillerConfigManager(IDbContextFactory<CoreDatabaseContext> dbContextFactory) : base(dbContextFactory)
  {

  }
}
