using Ares.Core.Device.State.Logging;
using AresService.Data;
using LaserChiller;
using Microsoft.EntityFrameworkCore;

namespace AresService.DeviceStateLoggers.LaserChiller;

public class LaserChillerStateLoggerFactory : DeviceStateLoggerFactory<ILaserChiller>
{
  readonly IDbContextFactory<AresDbContext> _dbContextFactory;
  public LaserChillerStateLoggerFactory(IDbContextFactory<AresDbContext> dbContextFactory)
  {
    _dbContextFactory = dbContextFactory;
  }

  protected override IDeviceStateLogger Create(ILaserChiller laserChiller)
    => new LaserChillerStateLogger(_dbContextFactory, laserChiller);
}
