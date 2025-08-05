using Microsoft.EntityFrameworkCore;
using RestSerialDevice;

namespace AresService.DeviceStateLoggers.RestDevice;

public class RestDeviceLoggerFactory : IDeviceStateLoggerFactory<ISerialRestDevice, IRestDeviceStateLogger>
{
  readonly IDbContextFactory<AresDbContext> _dbContextFactory;
  public RestDeviceLoggerFactory(IDbContextFactory<AresDbContext> dbContextFactory)
  {
    _dbContextFactory = dbContextFactory;
  }

  public IRestDeviceStateLogger Create(ISerialRestDevice restDevice)
    => new RestDeviceStateLogger(_dbContextFactory, restDevice);
}
