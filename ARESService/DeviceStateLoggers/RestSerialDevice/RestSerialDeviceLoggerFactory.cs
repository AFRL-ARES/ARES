using Ares.Core.Device.State.Logging;
using Microsoft.EntityFrameworkCore;
using RestSerialDevice;

namespace AresService.DeviceStateLoggers.RestSerialDevice;

public class RestSerialDeviceLoggerFactory : IDeviceStateLoggerFactory<ISerialRestDevice, IRestSerialDeviceStateLogger>
{
  readonly IDbContextFactory<AresDbContext> _dbContextFactory;
  public RestSerialDeviceLoggerFactory(IDbContextFactory<AresDbContext> dbContextFactory)
  {
    _dbContextFactory = dbContextFactory;
  }

  public IRestSerialDeviceStateLogger Create(ISerialRestDevice restDevice)
    => new RestSerialDeviceStateLogger(_dbContextFactory, restDevice);
}
