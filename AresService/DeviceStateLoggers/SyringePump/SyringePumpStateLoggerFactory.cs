using Ares.Core.Device.State.Logging;
using AresService.Data;
using Microsoft.EntityFrameworkCore;
using SyringePumpNE1000;

namespace AresService.DeviceStateLoggers.SyringePump;
public class SyringePumpStateLoggerFactory : DeviceStateLoggerFactory<ISyringePump>
{
  readonly IDbContextFactory<AresDbContext> _dbContextFactory;
  public SyringePumpStateLoggerFactory(IDbContextFactory<AresDbContext> dbContextFactory)
  {
    _dbContextFactory = dbContextFactory;
  }

  protected override IDeviceStateLogger Create(ISyringePump syringePump)
    => new SyringePumpStateLogger(_dbContextFactory, syringePump);
}
