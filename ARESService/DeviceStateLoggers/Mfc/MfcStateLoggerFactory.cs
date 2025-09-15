using AlicatMFC;
using Ares.Core.Device.State.Logging;
using Microsoft.EntityFrameworkCore;

namespace AresService.DeviceStateLoggers.Mfc;

public class MfcStateLoggerFactory : DeviceStateLoggerFactory<IMassFlowController>
{
  private readonly IDbContextFactory<AresDbContext> _dbContextFactory;

  public MfcStateLoggerFactory(IDbContextFactory<AresDbContext> dbContextFactory)
  {
    _dbContextFactory = dbContextFactory;
  }

  protected override IDeviceStateLogger Create(IMassFlowController device)
    => new MfcStateLogger(_dbContextFactory, device);
}
