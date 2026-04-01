using Ares.Core.Notifications;
using Ares.Core.Visualization.Repos;
using Ares.Datamodel.Visualizing;
using Ares.Datamodel.Visualizing.Local;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ares.Core.Visualization.Managers;

public class VisualizationConfigManager : IVisualizationConfigManager
{
  private readonly IDbContextFactory<CoreDatabaseContext> _dbContextFactory;
  private IDeviceVisualizationConfigRepo _deviceVisualizationConfigRepo;
  private readonly ILogger<VisualizationConfigManager> _logger;
  private INotificationHandler _notificationHandler;

  public VisualizationConfigManager(IDbContextFactory<CoreDatabaseContext> dbContextFactory,
    IDeviceVisualizationConfigRepo deviceVisualizationConfigRepo,
    ILogger<VisualizationConfigManager> logger,
    INotificationHandler notificationHandler)
  {
    _dbContextFactory = dbContextFactory;
    _deviceVisualizationConfigRepo = deviceVisualizationConfigRepo;
    _logger = logger;
    _notificationHandler = notificationHandler;
  }

  public async Task AddDeviceVisualization(string deviceId, VisualizationPath path, ChartStyle style)
  {
    var newConfig = new DeviceVisualizationConfig
    {
      UniqueId = Guid.NewGuid().ToString(),
      DeviceId = deviceId,
      Path = path,
      Style = style,
      NumberDisplayPoints = 20,
      PollingRate = 3000,
      ShowDataLabels = false
    };

    try
    {
      await using var context = _dbContextFactory.CreateDbContext();
      context.DeviceVisualizationConfigs.Add(newConfig);
      await context.SaveChangesAsync();
      _deviceVisualizationConfigRepo.AddOrUpdate(newConfig);
    }

    catch(Exception ex)
    {
      _logger.LogError($"Error occured when trying to add a new device visualization chart: {ex.Message}");
      await _notificationHandler.HandleNotification("Failed to Add Visualization", "Device visualization could not be created, check logs for additional details.", NotificationSeverityEnum.Error);
    }
  }

  public async Task LoadConfigs()
  {
    await using var context = _dbContextFactory.CreateDbContext();
    var existingConfigs = await context.DeviceVisualizationConfigs.ToListAsync();
    existingConfigs.ForEach(_deviceVisualizationConfigRepo.AddOrUpdate);
  }

  public async Task Remove(string configId)
  {
    await using var context = _dbContextFactory.CreateDbContext();
    var genericConfig = await context.DeviceVisualizationConfigs.FirstOrDefaultAsync(config => config.UniqueId == configId);
    if(genericConfig is null)
      return;

    context.DeviceVisualizationConfigs.Remove(genericConfig);
    await context.SaveChangesAsync();
    _deviceVisualizationConfigRepo.Remove(configId);
  }

  public async Task UpdateDeviceVisualization(string configId, DeviceVisualizationConfig config)
  {
    await using var context = _dbContextFactory.CreateDbContext();
    var existingConfig = await context.DeviceVisualizationConfigs.FirstOrDefaultAsync(c => c.UniqueId == configId);
    if(existingConfig is null)
      return;

    existingConfig.DeviceId = config.DeviceId;
    existingConfig.Style = config.Style;
    existingConfig.Path = config.Path;
    existingConfig.PollingRate = config.PollingRate;
    existingConfig.ShowDataLabels = config.ShowDataLabels;
    existingConfig.NumberDisplayPoints = config.NumberDisplayPoints;

    await context.SaveChangesAsync();
    _deviceVisualizationConfigRepo.AddOrUpdate(existingConfig);
  }
}
