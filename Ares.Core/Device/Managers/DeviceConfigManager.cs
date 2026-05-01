using Ares.Core.Device.Plugins.Drivers;
using Ares.Core.Device.Providers;
using Ares.Core.Device.Repos;
using Ares.Core.Notifications;
using Ares.Datamodel.Device;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ares.Core.Device.Managers;

public class DeviceConfigManager : IDeviceConfigManager
{
  private readonly IDbContextFactory<CoreDatabaseContext> _dbContextFactory;
  private readonly IDriverDatabaseManager _driverDbManager;
  private readonly IDeviceDriverProvider _driverProvider;
  private readonly ILogger<DeviceConfigManager> _logger;
  private readonly INotificationHandler _notificationHandler;
  private IDeviceConfigRepo _configRepo;

  public DeviceConfigManager(IDbContextFactory<CoreDatabaseContext> dbContextFactory, 
    IDriverDatabaseManager driverDbManager, 
    IDeviceDriverProvider driverProvider, 
    IDeviceConfigRepo configRepo,
    ILogger<DeviceConfigManager> logger,
    INotificationHandler notificationHandler)
  {
    _dbContextFactory = dbContextFactory;
    _configRepo = configRepo;
    _driverDbManager = driverDbManager;
    _driverProvider = driverProvider;
    _notificationHandler = notificationHandler;
    _logger = logger;
  }

  public async Task LoadConfigs()
  {
    await using var context = _dbContextFactory.CreateDbContext();
    await HandleMissingDriver(context);
    var existingDeviceConfigs = await context.DeviceConfigs.ToListAsync();
    existingDeviceConfigs.ForEach(_configRepo.AddOrUpdate);
  }

  public async Task Add(DeviceConfig config)
  {
    //Initialize important ID's
    config.UniqueId = Guid.NewGuid().ToString();
    config.DeviceId = Guid.NewGuid().ToString();

    //Add to storage mechanisms
    await using var context = _dbContextFactory.CreateDbContext();
    var existingDeviceConfig = await context.DeviceConfigs.FirstOrDefaultAsync(existingConfig => existingConfig.UniqueId == config.UniqueId);
    if(existingDeviceConfig is not null)
      throw new InvalidOperationException($"A device with id {config.UniqueId} already exists in the configuration database");

    context.DeviceConfigs.Add(config);
    await context.SaveChangesAsync();
    _configRepo.AddOrUpdate(config);
  }

  public async Task Remove(string id)
  {
    await using var context = _dbContextFactory.CreateDbContext();
    var genericConfig = await context.DeviceConfigs.FirstOrDefaultAsync(config => config.UniqueId == id);
    if(genericConfig is null)
      return;

    context.DeviceConfigs.Remove(genericConfig);
    await context.SaveChangesAsync();
    _configRepo.Remove(id);
  }

  public async Task Update(string id, DeviceConfig config)
  {
    await using var context = _dbContextFactory.CreateDbContext();
    var existingConfig = await context.DeviceConfigs.FirstOrDefaultAsync(c => c.UniqueId == id);
    if(existingConfig is null)
      return;

    existingConfig.DeviceName = config.DeviceName;
    existingConfig.SerialInfo = config.SerialInfo;
    existingConfig.DeviceSettings = config.DeviceSettings;
    existingConfig.DriverId = config.DriverId;
    existingConfig.IsSimulated = config.IsSimulated;

    await context.SaveChangesAsync();
    _configRepo.AddOrUpdate(existingConfig);
  }

  private async Task HandleMissingDriver(CoreDatabaseContext context)
  {
    var configs = await context.DeviceConfigs.ToListAsync();
    var archivedDrivers = await _driverDbManager.GetAllDrivers();
    var currentDrivers = _driverProvider.GetAllDeviceDrivers();

    var currentDriverIds = currentDrivers.Select(d => d.UniqueId).ToHashSet();
    var archivedDriverMap = archivedDrivers.ToDictionary(d => d.DriverId);

    // Track if we made any migrations so we can save the DB context once at the end
    bool hasUpdates = false;

    foreach(var config in configs)
    {
      // Potentially artifact of DB migration, remove and ignore.
      if(config.DriverId is null)
      {
        _logger.LogWarning("ARES detected a device with a null driver ID. If you recently migrated to a new database this may be normal. {DeviceName}", config.DeviceName);
        var genericConfig = await context.DeviceConfigs.FirstOrDefaultAsync(config => config.UniqueId == config.UniqueId);
        if(genericConfig is null)
          return;

        context.DeviceConfigs.Remove(genericConfig);
        continue;
      }

      // Driver found, no action needed
      if(currentDriverIds.Contains(config.DriverId))
        continue;

      // Driver missing. Check the archive for a migration path.
      if(archivedDriverMap.TryGetValue(config.DriverId, out var archivedDriver))
      {
        _logger.LogInformation("ARES detected a missing driver for the device {DeviceName}. Failed to find a matching driver the the Driver ID of {driver_id}", config.DeviceName, config.DriverId);
        var currentMatch = currentDrivers.FirstOrDefault(cd => cd.Manifest.DeviceTypeName == archivedDriver.DisplayName);

        if(currentMatch is not null)
        {
          _logger.LogInformation("ARES found a replacement driver for the device {DeviceName} and will update that device to use this new driver. New driver ID: {DriverID}", config.DeviceName, config.DriverId);
          // Migration successful: map to the new driver and skip deletion
          config.DriverId = currentMatch.UniqueId;
          hasUpdates = true;
          continue;
        }

        // Migration failed: Archive exists, but no replacement driver found
        var noNewDriverMessage = $"ARES detected the driver for '{config.DeviceName}' was deleted. An archived driver was found, but a replacement could not be located. " +
                                 "To avoid the presence of ghost devices, ARES has deleted this device from your system.";

        _logger.LogWarning(noNewDriverMessage);
        await _notificationHandler.HandleNotification("Device Automatically Deleted", noNewDriverMessage, NotificationSeverityEnum.Warning);
        var genericConfig = await context.DeviceConfigs.FirstOrDefaultAsync(config => config.UniqueId == config.UniqueId);
        if(genericConfig is null)
          return;

        context.DeviceConfigs.Remove(genericConfig);
        continue;
      }

      // Driver missing AND no archive record exists
      var message = $"ARES detected the driver for '{config.DeviceName}' was deleted, and no reference of this device's driver was found in the driver archive. " +
                    "To avoid the presence of ghost devices, ARES has deleted this device from your system.";

      _logger.LogWarning(message);
      await _notificationHandler.HandleNotification("Device Automatically Deleted", message, NotificationSeverityEnum.Warning);
      await Remove(config.UniqueId);
    }

    // Persist any driver ID updates to the database
    if(hasUpdates)
      await context.SaveChangesAsync();
  }
}
