using ARESCore;
using Microsoft.EntityFrameworkCore;
using SyringePumpNE1000;

namespace ARESCore.DeviceStateLoggers.SyringePump;
public class SyringePumpStateLoggerFactory : IDeviceStateLoggerFactory<ISyringePump, ISyringePumpStateLogger>
{
  readonly IDbContextFactory<ARESDbContext> _dbContextFactory;
  public SyringePumpStateLoggerFactory(IDbContextFactory<ARESDbContext> dbContextFactory)
  {
    _dbContextFactory = dbContextFactory;
  }

  public ISyringePumpStateLogger Create(ISyringePump syringePump)
    => new SyringePumpStateLogger(_dbContextFactory, syringePump);
}
