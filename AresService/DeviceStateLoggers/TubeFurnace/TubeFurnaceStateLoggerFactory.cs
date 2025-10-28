using Ares.Core.Device.State.Logging;
using AresService.Data;
using LindbergFurnace;
using Microsoft.EntityFrameworkCore;

namespace AresService.DeviceStateLoggers.TubeFurnace
{
  public class TubeFurnaceStateLoggerFactory : DeviceStateLoggerFactory<ITubeFurnace>
  {
    readonly IDbContextFactory<AresDbContext> _dbContextFactory;
    public TubeFurnaceStateLoggerFactory(IDbContextFactory<AresDbContext> dbContextFactory)
    {
      _dbContextFactory = dbContextFactory;
    }

    protected override IDeviceStateLogger Create(ITubeFurnace tubeFurnace)
      => new TubeFurnaceStateLogger(_dbContextFactory, tubeFurnace);
  }
}
