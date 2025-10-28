using Ares.Core.Device.State.Logging;
using AresService.Data;
using Microsoft.EntityFrameworkCore;
using RestSerialDevice;

namespace AresService.DeviceStateLoggers.RestDevice;

public class RestDeviceLoggerFactory : DeviceStateLoggerFactory<ISerialRestDevice>
{
  readonly IDbContextFactory<AresDbContext> _dbContextFactory;
  public RestDeviceLoggerFactory(IDbContextFactory<AresDbContext> dbContextFactory)
  {
    _dbContextFactory = dbContextFactory;
  }

  protected override IDeviceStateLogger Create(ISerialRestDevice restDevice)
    => new RestDeviceStateLogger(_dbContextFactory, restDevice);
}
