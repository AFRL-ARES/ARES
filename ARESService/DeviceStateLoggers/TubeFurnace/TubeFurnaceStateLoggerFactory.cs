using LindbergFurnace;
using Microsoft.EntityFrameworkCore;

namespace AresService.DeviceStateLoggers.TubeFurnace
{
  public class TubeFurnaceStateLoggerFactory : IDeviceStateLoggerFactory<ITubeFurnace, ITubeFurnaceStateLogger>
  {
    readonly IDbContextFactory<AresDbContext> _dbContextFactory;
    public TubeFurnaceStateLoggerFactory(IDbContextFactory<AresDbContext> dbContextFactory)
    {
      _dbContextFactory = dbContextFactory;
    }

    public ITubeFurnaceStateLogger Create(ITubeFurnace tubeFurnace)
      => new TubeFurnaceStateLogger(_dbContextFactory, tubeFurnace);
  }
}
