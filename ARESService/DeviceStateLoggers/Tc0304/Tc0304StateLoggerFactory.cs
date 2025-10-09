using Ares.Core.Device.State.Logging;
using AresService.Data;
using Microsoft.EntityFrameworkCore;
using TC0304;

namespace AresService.DeviceStateLoggers.Tc0304;
public class Tc0304StateLoggerFactory : DeviceStateLoggerFactory<IDataloggerThermometer>
{
  readonly IDbContextFactory<AresDbContext> _dbContextFactory;
  public Tc0304StateLoggerFactory(IDbContextFactory<AresDbContext> dbContextFactory)
  {
    _dbContextFactory = dbContextFactory;
  }

  protected override IDeviceStateLogger Create(IDataloggerThermometer dataloggerThermometer)
    => new Tc0304StateLogger(_dbContextFactory, dataloggerThermometer);
}
