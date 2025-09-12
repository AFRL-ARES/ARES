using Ares.Core.Device.State.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ares.Core.Device.Remote.State;
public class RemoteDeviceStateLoggerFactory : IDeviceStateLoggerFactory<RemoteDevice, RemoteDeviceStateLogger>
{
  private readonly ILoggerFactory _loggerFactory;
  private readonly IDbContextFactory<CoreDatabaseContext> _dbContextFactory;

  public RemoteDeviceStateLoggerFactory(ILoggerFactory loggerFactory, IDbContextFactory<CoreDatabaseContext> dbContextFactory)
  {
    _loggerFactory = loggerFactory;
    _dbContextFactory = dbContextFactory;
  }

  public RemoteDeviceStateLogger Create(RemoteDevice device)
  {
    return new RemoteDeviceStateLogger(_dbContextFactory, device, _loggerFactory.CreateLogger<RemoteDeviceStateLogger>());
  }
}
