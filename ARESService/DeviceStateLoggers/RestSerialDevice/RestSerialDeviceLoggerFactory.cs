using Ares.Core.Device.State.Logging;
using Microsoft.EntityFrameworkCore;
using RestSerialDevice;

namespace AresService.DeviceStateLoggers.RestSerialDevice;

public class RestSerialDeviceLoggerFactory : DeviceStateLoggerFactory<ISerialRestDevice>
{
  readonly IDbContextFactory<AresDbContext> _dbContextFactory;
  public RestSerialDeviceLoggerFactory(IDbContextFactory<AresDbContext> dbContextFactory)
  {
    _dbContextFactory = dbContextFactory;
  }

  protected override IDeviceStateLogger Create(ISerialRestDevice restDevice)
    => new RestSerialDeviceStateLogger(_dbContextFactory, restDevice);
}
