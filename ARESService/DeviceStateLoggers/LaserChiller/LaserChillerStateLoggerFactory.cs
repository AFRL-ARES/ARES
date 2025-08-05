using LaserChiller;
using Microsoft.EntityFrameworkCore;

namespace AresService.DeviceStateLoggers.LaserChiller;

public class LaserChillerStateLoggerFactory : IDeviceStateLoggerFactory<ILaserChiller, ILaserChillerStateLogger>
{
  readonly IDbContextFactory<AresDbContext> _dbContextFactory;
  public LaserChillerStateLoggerFactory(IDbContextFactory<AresDbContext> dbContextFactory)
  {
    _dbContextFactory = dbContextFactory;
  }

  public ILaserChillerStateLogger Create(ILaserChiller laserChiller)
    => new LaserChillerStateLogger(_dbContextFactory, laserChiller);
}
