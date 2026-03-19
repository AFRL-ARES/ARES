using Ares.Device;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ares.Core.Device.State.Logging;

public class AresDeviceStateLoggerFactory : IDeviceStateLoggerFactory
{
  private readonly IDbContextFactory<CoreDatabaseContext> _dbContextFactory;
  private readonly ILoggerFactory _loggerFactory;

  public AresDeviceStateLoggerFactory(ILoggerFactory loggerFactory, IDbContextFactory<CoreDatabaseContext> dbContextFactory)
  {
    _dbContextFactory = dbContextFactory;
    _loggerFactory = loggerFactory;
  }

  public IDeviceStateLogger Create(IAresDevice device)
  {
    return new AresDeviceStateLogger(_dbContextFactory, device, _loggerFactory.CreateLogger<AresDeviceStateLogger>());
  }
}
