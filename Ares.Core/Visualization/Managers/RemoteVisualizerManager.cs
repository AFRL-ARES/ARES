using Ares.Core.Execution.VersionChecking;
using Ares.Core.Notifications;
using Ares.Core.Visualization.Repos;
using Ares.Datamodel.Analyzing;
using Ares.Datamodel.Visualizing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Xml.Linq;

namespace Ares.Core.Visualization.Managers;

public class RemoteVisualizerManager : IRemoteVisualizerManager
{
  private readonly IDbContextFactory<CoreDatabaseContext> _dbContextFactory;
  private readonly IVisualizerRepo _visualizerRepo;
  private readonly INotificationHandler _notificationHandler;
  private readonly IVisualizerCache _visualizerCache;
  private readonly IDatamodelVersionValidator _datamodelVersionValidator;
  private readonly List<RemoteVisualizerMonitor> _visualizerMonitors = [];
  private readonly ILogger<RemoteVisualizerManager> _logger;

  public RemoteVisualizerManager(
    IDbContextFactory<CoreDatabaseContext> dbContextFactory,
    IVisualizerRepo visualizerRepo,
    INotificationHandler notificationHandler,
    IVisualizerCache visualizerCache,
    IDatamodelVersionValidator datamodelVersionValidator,
    ILogger<RemoteVisualizerManager> logger)
  {
    _dbContextFactory = dbContextFactory;
    _visualizerRepo = visualizerRepo;
    _notificationHandler = notificationHandler;
    _visualizerCache = visualizerCache;
    _datamodelVersionValidator = datamodelVersionValidator;
    _logger = logger;
  }

  public async Task CreateVisualizer(string name, string url)
  {
    var config = new VisualizerConfig
    {
      UniqueId = Guid.NewGuid().ToString(),
      Name = name,
      Url = url
    };

    var visualizer = ConfigToVisualizer(config);
    if(visualizer is null)
      return;

    _visualizerRepo.AddVisualizer(visualizer);
    _visualizerMonitors.Add(new RemoteVisualizerMonitor(visualizer, _visualizerCache));

    await using var context = await _dbContextFactory.CreateDbContextAsync();
    context.Visualizers.Add(config);
    await context.SaveChangesAsync();
    _logger.LogInformation("Successfully Created a New Visualizer: {visName}", name);
  }

  public async Task LoadVisualizers()
  {
    _logger.LogDebug("Loading Remote Visualizers");

    await using var context = await _dbContextFactory.CreateDbContextAsync();
    var configs = await context.Visualizers.ToArrayAsync();
    _logger.LogDebug("Found {count} Visualizer Configs in the Database", configs.Length);

    var visualizers = await Task.WhenAll(configs.Select(LoadExistingVisualizer));
    _logger.LogDebug("Successfully Loaded {count} Visualizers from Database", visualizers.Length);

    var visualizerList = visualizers.OfType<RemoteVisualizer>().ToArray();

    if(visualizerList.Length == 0 && configs.Length > 0)
      _logger.LogWarning("Visualizer configs were found, but zero could be parsed into a RemoteVisualizer type.");

    foreach(var visualizer in visualizerList)
    {
      _visualizerRepo.AddVisualizer(visualizer);
      _visualizerMonitors.Add(new RemoteVisualizerMonitor(visualizer, _visualizerCache));
    }

    _logger.LogInformation("Successfully Loaded Visualizers from the Database");
  }

  public async Task RemoveVisualizer(string visualizerId)
  {
    _logger.LogDebug("Received a Visualizer Removal Request");
    await using var context = await _dbContextFactory.CreateDbContextAsync();
    var config = await context.Visualizers.FirstOrDefaultAsync(visualizer => visualizer.UniqueId == visualizerId);

    if(config is null)
    {
      _logger.LogWarning("Failed to find a config matching the ID of {configId} in the database", visualizerId);
      return;
    }
      
    context.Visualizers.Remove(config);
    await context.SaveChangesAsync();
    DisposeVisualizer(visualizerId);

    _logger.LogInformation("Successfully Removed Visualizer with an ID of {visId}", visualizerId);
  }

  public async Task UpdateVisualizer(VisualizerConfig config)
  {
    _logger.LogDebug("Received a Visualization Update Request for {name}", config.Name);

    await using var context = await _dbContextFactory.CreateDbContextAsync();
    var existingConfig = await context.Visualizers.FirstOrDefaultAsync(visualizer => visualizer.UniqueId == config.UniqueId);
    if(existingConfig is null)
    {
      _logger.LogWarning("Visualization Config Update Aborted, No Matching Config Found for {name} with ID of {id}", config.Name, config.UniqueId);
      return;
    }

    existingConfig.Name = config.Name;
    existingConfig.Url = config.Url;
    await context.SaveChangesAsync();

    DisposeVisualizer(existingConfig.UniqueId);

    var visualizer = await LoadExistingVisualizer(existingConfig);
    if(visualizer is null)
      return;
      

    _visualizerRepo.AddVisualizer(visualizer);
    _visualizerMonitors.Add(new RemoteVisualizerMonitor(visualizer, _visualizerCache));

    _logger.LogInformation("Successfully Updated Visualizer Service: {name}", config.Name);
  }

  public Task UpdateVisualizerSettings(VisualizerSettings visualizerSettings)
  {
    _logger.LogDebug("Received an Update Visualizer Settings Request");
    var visualizer = _visualizerRepo.GetVisualizerById(visualizerSettings.VisualizerId);
    if(visualizer is null)
    {
      _logger.LogWarning("Visualizer Settings Update Aborted, Failed to find Visualizer Matching Provided ID of {id}", visualizerSettings.VisualizerId);
      return Task.CompletedTask;
    }

    visualizer.UpdateSettings(visualizerSettings.Settings);

    if(visualizer is not RemoteVisualizer remoteVisualizer)
    {
      _logger.LogWarning("Found Visualizer Matching Provided ID of {id}, but was of the incorrect type.", visualizerSettings.VisualizerId);
      return Task.CompletedTask;
    }
      
    _logger.LogInformation("Updated Settings for Visualizer {name}", visualizer.Name);
    return _visualizerCache.CacheVisualizerSettings(remoteVisualizer);
  }

  private RemoteVisualizer? ConfigToVisualizer(VisualizerConfig config)
  {
    var uriValid = Uri.TryCreate(config.Url, UriKind.Absolute, out var uri);
    if(!uriValid || uri is null)
    {
      _ = _notificationHandler.HandleNotification(
        "Visualizer Load Error",
        $"Failed to load a remote visualizer {config.Name} because the url {config.Url} is invalid.",
        NotificationSeverityEnum.Danger);

      _logger.LogError("Failed to Load Visualizer {name} as the provided URL ({url}) was invalid", config.Name, config.Url);
      return null;
    }

    return new RemoteVisualizer(config.Name, uri, _datamodelVersionValidator, config.UniqueId);
  }

  private async Task<RemoteVisualizer?> LoadExistingVisualizer(VisualizerConfig config)
  {
    _logger.LogDebug("Loading Visualizer {name} with an ID of {id}", config.Name, config.UniqueId);
    var visualizer = ConfigToVisualizer(config);
    if(visualizer is null)
      return null;

    try
    {
      var visualizerInfo = await _visualizerCache.GetCachedVisualizerInfo(config.UniqueId);
      if(visualizerInfo is not null)
      {
        _logger.LogDebug("Found Cached Visualizer Info for Visualizer with ID of {id}, Updating Info", config.UniqueId);
        await visualizer.UpdateInfo(visualizerInfo);
      }

      await visualizer.Init();

      var visualizerSettings = await _visualizerCache.GetCachedVisualizerSettings(config.UniqueId);
      if(visualizerSettings is not null)
        visualizer.UpdateSettings(visualizerSettings);

      await _visualizerCache.CacheVisualizerInfo(visualizer);
      await _visualizerCache.CacheVisualizerSettings(visualizer);
    }
    catch(Exception e)
    {
      await visualizer.SetOfflineVisualizerStatus(e.Message);
      _logger.LogError(e, "Failed to Load Visualizer {name}", config.Name);
    }

    return visualizer;
  }

  private void DisposeVisualizer(string visualizerId)
  {
    _logger.LogDebug("Disposing of Visualizer with ID of {id}", visualizerId);
    var visualizer = _visualizerRepo.GetVisualizerById(visualizerId);
    
    if(visualizer is null)
    {
      _logger.LogWarning("Aborting Visualizer Disposal, Visualizer with ID of {id} Not Found", visualizerId);
      return;
    }
      
    _visualizerRepo.RemoveVisualizer(visualizer);

    var monitor = _visualizerMonitors.FirstOrDefault(monitor => monitor.VisualizerId == visualizerId);
    if(monitor is not null)
    {
      _logger.LogDebug("Disposing of Visualiztion Monitor for Visualizer with ID of {id}", visualizerId);
      monitor.Dispose();
      _visualizerMonitors.Remove(monitor);
    }

    else
      _logger.LogWarning("Aborting Visualizer Monitor Disposal, No Monitor Found for Visualizer with ID of {id}", visualizerId);

    if(visualizer is IDisposable disposable)
      disposable.Dispose();

    _logger.LogInformation("Successfully Disposed of Visualizer with ID of {id}", visualizerId);
  }
}