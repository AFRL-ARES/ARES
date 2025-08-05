using Microsoft.EntityFrameworkCore;
using SyringePumpNE1000;

namespace AresService.DeviceStateLoggers.SyringePump;
public class SyringePumpStateLoggerFactory : IDeviceStateLoggerFactory<ISyringePump, ISyringePumpStateLogger>
{
  readonly IDbContextFactory<AresDbContext> _dbContextFactory;
  public SyringePumpStateLoggerFactory(IDbContextFactory<AresDbContext> dbContextFactory)
  {
    _dbContextFactory = dbContextFactory;
  }

  public ISyringePumpStateLogger Create(ISyringePump syringePump)
    => new SyringePumpStateLogger(_dbContextFactory, syringePump);
}
