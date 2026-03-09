using Ares.Services;

namespace Ares.Core.Device.Plugins.Drivers;

public interface IDriverDatabaseManager
{
  Task AddOrUpdateDeviceDriver(DeviceDriver driver);
  Task RemoveDeviceDriver(DeviceDriver driver);
  Task RefreshDriverArchive();
  Task<IEnumerable<DriverInfo>> GetAllDrivers();
}
