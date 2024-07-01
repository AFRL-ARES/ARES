using ARESCore;
using Microsoft.EntityFrameworkCore;
using TC0304;

namespace ARESCore.DeviceStateLoggers.Tc0304;
public class Tc0304StateLoggerFactory : IDeviceStateLoggerFactory<IDataloggerThermometer, ITc0304StateLogger>
{
  readonly IDbContextFactory<ARESDbContext> _dbContextFactory;
  public Tc0304StateLoggerFactory(IDbContextFactory<ARESDbContext> dbContextFactory)
  {
    _dbContextFactory = dbContextFactory;
  }

  public ITc0304StateLogger Create(IDataloggerThermometer dataloggerThermometer)
    => new Tc0304StateLogger(_dbContextFactory, dataloggerThermometer);
}
