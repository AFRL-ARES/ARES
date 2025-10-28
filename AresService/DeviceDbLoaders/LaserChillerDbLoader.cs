using AresService.Data;
using AresService.DeviceManagers;
using Chiller.Config;
using LaserChiller;
using Microsoft.EntityFrameworkCore;

namespace AresService.DeviceDbLoaders;

public class LaserChillerDbLoader : DeviceDbLoaderBase<ILaserChiller, ChillerConfig>
{
  public LaserChillerDbLoader(IDbContextFactory<AresDbContext> dbContextFactory, IDeviceManager<ChillerConfig, ILaserChiller> deviceManager) : base(dbContextFactory, deviceManager)
  {

  }
}
