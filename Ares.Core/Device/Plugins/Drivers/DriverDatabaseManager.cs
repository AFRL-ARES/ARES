
using Ares.Core.Device.Providers;
using Ares.Services;
using Microsoft.EntityFrameworkCore;

namespace Ares.Core.Device.Plugins.Drivers;

public class DriverDatabaseManager : IDriverDatabaseManager
{
  private readonly IDbContextFactory<CoreDatabaseContext> _dbContextFactory;
  private readonly IDeviceDriverProvider _driverProvider;

  public DriverDatabaseManager(IDbContextFactory<CoreDatabaseContext> dbContextFactory, IDeviceDriverProvider driverProvider)
  {
    _dbContextFactory = dbContextFactory;
    _driverProvider = driverProvider;
  }

  public async Task AddOrUpdateDeviceDriver(DeviceDriver driver)
  {
    await using var context = _dbContextFactory.CreateDbContext();
    var newDriver = new DriverInfo()
    {
      DriverId = driver.UniqueId,
      FileSizeBytes = driver.DriverSize,
      Version = driver.Manifest.Version,
      DisplayName = driver.Manifest.DeviceTypeName
    };

    var matchingDriver = context.DeviceDrivers.FirstOrDefault(d => d.DriverId == driver.UniqueId);

    if(matchingDriver != null)
      matchingDriver = newDriver;

    else
      await context.DeviceDrivers.AddAsync(newDriver);  

    await context.SaveChangesAsync();
  }

  public async Task<IEnumerable<DriverInfo>> GetAllDrivers()
  {
    await using var context = _dbContextFactory.CreateDbContext();
    return await context.DeviceDrivers.ToArrayAsync();
  }

  public async Task RefreshDriverArchive()
  {
    var currentDrivers = _driverProvider.GetAllDeviceDrivers();

    await using var context = _dbContextFactory.CreateDbContext();
    await context.DeviceDrivers.ExecuteDeleteAsync();
    foreach(var driver in currentDrivers)
    {
      await context.AddAsync(new DriverInfo
      {
        DriverId = driver.UniqueId,
        FileSizeBytes = driver.DriverSize,
        Version = driver.Manifest.Version,
        DisplayName = driver.Manifest.DeviceTypeName
      });
    }
    await context.SaveChangesAsync();
  }

  public async Task RemoveDeviceDriver(DeviceDriver driver)
  {
    await using var context = _dbContextFactory.CreateDbContext();
    
    var matchingDriver = await context.DeviceDrivers.FirstOrDefaultAsync(d => d.DriverId == driver.UniqueId);

    if(matchingDriver is null)
      return;

    context.DeviceDrivers.Remove(matchingDriver);
    await context.SaveChangesAsync();
  }
}
