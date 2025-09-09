using AlicatMFC;
using Ares.Core.Device.State.Logging;
using Microsoft.EntityFrameworkCore;

namespace AresService.DeviceStateLoggers.Mfc;

public class MfcStateLoggerFactory : IDeviceStateLoggerFactory<IMassFlowController, IMfcStateLogger>
{
  private readonly IDbContextFactory<AresDbContext> _dbContextFactory;

  public MfcStateLoggerFactory(IDbContextFactory<AresDbContext> dbContextFactory)
  {
    _dbContextFactory = dbContextFactory;
  }

  public IMfcStateLogger Create(IMassFlowController device)
    => new MfcStateLogger(_dbContextFactory, device);
}
