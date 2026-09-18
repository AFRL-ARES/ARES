using Ares.Core.Device.Providers;
using Ares.Core.Device.Repos;
using Ares.Core.Notifications;
using Ares.Datamodel.Device;
using Microsoft.Extensions.Logging;

namespace Ares.Core.Device.Managers;

/// <summary>
/// Demo-mode implementation of <see cref="IDeviceConfigManager"/>.
/// Seeds the in-memory device config repository with static demo
/// plugin-based devices and never touches the configuration database.
/// </summary>
public class DemoDeviceConfigManager : IDeviceConfigManager
{
  private readonly IDeviceDriverProvider _driverProvider;
  private readonly IDeviceConfigRepo _configRepo;
  private readonly ILogger<DemoDeviceConfigManager> _logger;
  private readonly INotificationHandler _notificationHandler;

  public DemoDeviceConfigManager(
    IDeviceDriverProvider driverProvider,
    IDeviceConfigRepo configRepo,
    ILogger<DemoDeviceConfigManager> logger,
    INotificationHandler notificationHandler)
  {
    _driverProvider = driverProvider;
    _configRepo = configRepo;
    _logger = logger;
    _notificationHandler = notificationHandler;
  }

  public async Task LoadConfigs()
  {
    var drivers = _driverProvider.GetAllDeviceDrivers();
    if (!drivers.Any())
    {
      _logger.LogWarning("DemoDeviceConfigManager detected no loaded device drivers. Demo devices will not be available.");
      return;
    }

    var demoConfigs = DemoDeviceConfigFactory.CreateDemoConfigs(_driverProvider);
    if (!demoConfigs.Any())
    {
      _logger.LogWarning("DemoDeviceConfigManager could not resolve any demo device drivers. Demo devices will not be available.");
      return;
    }

    foreach (var config in demoConfigs)
    {
      _configRepo.AddOrUpdate(config);
      _logger.LogInformation("Registered demo device config {DeviceName} ({DeviceId})", config.DeviceName, config.DeviceId);
    }

    await Task.CompletedTask;
  }

  public async Task Add(DeviceConfig config)
  {
    // Demo mode: allow session-only additions, no DB persistence.
    config.UniqueId = string.IsNullOrWhiteSpace(config.UniqueId)
      ? Guid.NewGuid().ToString()
      : config.UniqueId;

    if (string.IsNullOrWhiteSpace(config.DeviceId))
      config.DeviceId = Guid.NewGuid().ToString();

    _configRepo.AddOrUpdate(config);
    _logger.LogInformation("Demo mode added device config {DeviceName} ({DeviceId})", config.DeviceName, config.DeviceId);

    await Task.CompletedTask;
  }

  public async Task Remove(string id)
  {
    var existing = _configRepo.GetConfig(id);
    if (existing is null)
      return;

    _configRepo.Remove(id);
    _logger.LogInformation("Demo mode removed device config {DeviceName} ({DeviceId})", existing.DeviceName, existing.DeviceId);

    await Task.CompletedTask;
  }

  public async Task Update(string id, DeviceConfig config)
  {
    var existing = _configRepo.GetConfig(id);
    if (existing is null)
    {
      _logger.LogWarning("Demo mode attempted to update non-existent device config with id {ConfigId}", id);
      return;
    }

    config.UniqueId = id;
    if (string.IsNullOrWhiteSpace(config.DeviceId))
      config.DeviceId = existing.DeviceId;

    _configRepo.AddOrUpdate(config);
    _logger.LogInformation("Demo mode updated device config {DeviceName} ({DeviceId})", config.DeviceName, config.DeviceId);

    await Task.CompletedTask;
  }
}
