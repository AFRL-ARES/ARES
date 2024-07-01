using ARESCore;
using LindbergFurnace;
using Microsoft.EntityFrameworkCore;

namespace ARESCore.DeviceStateLoggers.TubeFurnace;

public class TubeFurnaceStateLoggerFactory : IDeviceStateLoggerFactory<ITubeFurnace, ITubeFurnaceStateLogger>
{
  readonly IDbContextFactory<ARESDbContext> _dbContextFactory;
  public TubeFurnaceStateLoggerFactory(IDbContextFactory<ARESDbContext> dbContextFactory)
  {
    _dbContextFactory = dbContextFactory;
  }

  public ITubeFurnaceStateLogger Create(ITubeFurnace tubeFurnace)
    => new TubeFurnaceStateLogger(_dbContextFactory, tubeFurnace);
}
