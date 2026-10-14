using Ares.Core.Notifications;
using Ares.Core.Visualization.Repos;
using Ares.Datamodel.Visualizing.Remote;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ares.Core.Visualization.Managers;

public sealed class RemoteVisualizationConfigManager : IRemoteVisualizationConfigManager
{
  private readonly IDbContextFactory<CoreDatabaseContext> _dbContextFactory;
  private readonly IRemoteVisualizationConfigRepo _repo;
  private readonly ILogger<RemoteVisualizationConfigManager> _logger;
  private readonly INotificationHandler _notificationHandler;

  public RemoteVisualizationConfigManager(
    IDbContextFactory<CoreDatabaseContext> dbContextFactory,
    IRemoteVisualizationConfigRepo repo,
    ILogger<RemoteVisualizationConfigManager> logger,
    INotificationHandler notificationHandler)
  {
    _dbContextFactory = dbContextFactory;
    _repo = repo;
    _logger = logger;
    _notificationHandler = notificationHandler;
  }

  public async Task Initialize()
  {
    _logger.LogDebug("Initializing Remote Visualization Configurations");
    await using var context = await _dbContextFactory.CreateDbContextAsync();
    var configs = await context.RemoteVisualizationConfigs.ToArrayAsync();

    _logger.LogDebug("Found a total of {count} visualization configs in the database", configs.Length);
    foreach(var config in configs)
      _repo.AddOrUpdate(config);
  }

  public async Task Add(RemoteVisualizationConfig config)
  {
    if(config is null)
    {
      _logger.LogWarning("Received a null config, no visualization will be created");
      return;
    }

    _logger.LogDebug("Request Received to Add Remote Visualization Config with ID of {id}", config.UniqueId);
      
    if(string.IsNullOrWhiteSpace(config.UniqueId))
      config.UniqueId = Guid.NewGuid().ToString();

    try
    {
      await using var context = await _dbContextFactory.CreateDbContextAsync();
      context.RemoteVisualizationConfigs.Add(config);
      await context.SaveChangesAsync();
      _repo.AddOrUpdate(config);
      _logger.LogInformation("Successfully Added new Remote Visualization Config with ID of {id}", config.UniqueId);
    }
    catch(Exception exception)
    {
      _logger.LogError(exception, "Failed to add remote visualization configuration.");
      await _notificationHandler.HandleNotification(
        "Failed to Add Visualization",
        "The remote visualization could not be added. Check the logs for additional details.",
        NotificationSeverityEnum.Error);
    }
  }

  public async Task Remove(string configId)
  {
    _logger.LogDebug("Received a Removal Request for Remote Visualization Config with ID of {id}", configId);
    await using var context = await _dbContextFactory.CreateDbContextAsync();
    var config = await context.RemoteVisualizationConfigs.FirstOrDefaultAsync(item => item.UniqueId == configId);

    if(config is null)
    {
      _logger.LogWarning("Aborting Removal of Remote Visualization Config, Failed to Find Config Matching the ID of {id}", configId);
      return;
    }

    context.RemoteVisualizationConfigs.Remove(config);
    await context.SaveChangesAsync();
    _repo.Remove(configId);
    _logger.LogInformation("Successfully removed a visualization config with the ID of {id}", configId);
  }

  public async Task Update(RemoteVisualizationConfig config)
  {
    if(config is null)
    {
      _logger.LogWarning("Received a null config, no visualization updates will be applied");
      return;
    }

    _logger.LogDebug("Received an Update Request for Visualization Config with ID of {configId}", config.UniqueId);

    await using var context = await _dbContextFactory.CreateDbContextAsync();
    var existing = await context.RemoteVisualizationConfigs.FirstOrDefaultAsync(item => item.UniqueId == config.UniqueId);
    if(existing is null)
    {
      _logger.LogWarning("Aborting Remote Visualization Config Update, Failed to Find One Matching {id}", config.UniqueId);
      return;
    }

    existing.ChartTitle = config.ChartTitle;
    existing.VisualizerId = config.VisualizerId;
    existing.RequestedVisual = config.RequestedVisual;
    existing.GridX = config.GridX;
    existing.GridY = config.GridY;
    existing.GridW = config.GridW;
    existing.GridH = config.GridH;

    await context.SaveChangesAsync();
    _repo.AddOrUpdate(existing);

    _logger.LogInformation("Successfully Updated Remote Visualization Config for {name}", config.ChartTitle);
  }
}