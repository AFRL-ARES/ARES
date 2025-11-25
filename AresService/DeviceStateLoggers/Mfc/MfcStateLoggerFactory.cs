using AlicatMFC;
using Ares.Core.Device.State.Logging;
using AresService.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AresService.DeviceStateLoggers.Mfc;

public class MfcStateLoggerFactory : DeviceStateLoggerFactory<IMassFlowController>
{
  private readonly IDbContextFactory<AresDbContext> _dbContextFactory;
  private readonly ILoggerFactory _loggerFactory;

  public MfcStateLoggerFactory(IDbContextFactory<AresDbContext> dbContextFactory, ILoggerFactory loggerFactory)
  {
    _dbContextFactory = dbContextFactory;
    _loggerFactory = loggerFactory;
  }

  protected override IDeviceStateLogger Create(IMassFlowController device)
    => new MfcStateLogger(_dbContextFactory, device, _loggerFactory.CreateLogger<MfcStateLogger>());
}
