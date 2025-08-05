using Microsoft.EntityFrameworkCore;
using TC0304;

namespace AresService.DeviceStateLoggers.Tc0304;
public class Tc0304StateLoggerFactory : IDeviceStateLoggerFactory<IDataloggerThermometer, ITc0304StateLogger>
{
  readonly IDbContextFactory<AresDbContext> _dbContextFactory;
  public Tc0304StateLoggerFactory(IDbContextFactory<AresDbContext> dbContextFactory)
  {
    _dbContextFactory = dbContextFactory;
  }

  public ITc0304StateLogger Create(IDataloggerThermometer dataloggerThermometer)
    => new Tc0304StateLogger(_dbContextFactory, dataloggerThermometer);
}
