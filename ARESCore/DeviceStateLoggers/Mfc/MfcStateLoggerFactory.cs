using AlicatMFC;
using ARESCore;
using Microsoft.EntityFrameworkCore;

namespace ARESCore.DeviceStateLoggers.Mfc;

public class MfcStateLoggerFactory : IDeviceStateLoggerFactory<IMassFlowController, IMfcStateLogger>
{
  private readonly IDbContextFactory<ARESDbContext> _dbContextFactory;

  public MfcStateLoggerFactory(IDbContextFactory<ARESDbContext> dbContextFactory)
  {
    _dbContextFactory = dbContextFactory;
  }

  public IMfcStateLogger Create(IMassFlowController device)
    => new MfcStateLogger(_dbContextFactory, device);
}
