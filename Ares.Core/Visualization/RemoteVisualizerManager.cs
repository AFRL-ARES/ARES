using Ares.Core.Execution.VersionChecking;
using Ares.Core.Notifications;
using Ares.Datamodel.Analyzing;
using Ares.Datamodel.Visualizing;
using Microsoft.EntityFrameworkCore;

namespace Ares.Core.Visualization;

public class RemoteVisualizerManager : IRemoteVisualizerManager
{
  private readonly IDbContextFactory<CoreDatabaseContext> _dbContextFactory;
  private readonly IVisualizerRepo _visualizerRepo;
  private readonly INotificationHandler _notificationHandler;
  private readonly IVisualizerCache _visualizerCache;
  private readonly IDatamodelVersionValidator _datamodelVersionValidator;
  private readonly List<RemoteVisualizerMonitor> _visualizerMonitors = [];

  public RemoteVisualizerManager(
    IDbContextFactory<CoreDatabaseContext> dbContextFactory,
    IVisualizerRepo visualizerRepo,
    INotificationHandler notificationHandler,
    IVisualizerCache visualizerCache,
    IDatamodelVersionValidator datamodelVersionValidator)
  {
    _dbContextFactory = dbContextFactory;
    _visualizerRepo = visualizerRepo;
    _notificationHandler = notificationHandler;
    _visualizerCache = visualizerCache;
    _datamodelVersionValidator = datamodelVersionValidator;
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
  }

  public async Task LoadVisualizers()
  {
    await using var context = await _dbContextFactory.CreateDbContextAsync();
    var configs = await context.Visualizers.ToArrayAsync();
    var visualizers = await Task.WhenAll(configs.Select(LoadExistingVisualizer));

    foreach(var visualizer in visualizers.OfType<RemoteVisualizer>())
    {
      _visualizerRepo.AddVisualizer(visualizer);
      _visualizerMonitors.Add(new RemoteVisualizerMonitor(visualizer, _visualizerCache));
    }
  }

  public async Task RemoveVisualizer(string visualizerId)
  {
    await using var context = await _dbContextFactory.CreateDbContextAsync();
    var config = await context.Visualizers.FirstOrDefaultAsync(visualizer => visualizer.UniqueId == visualizerId);
    if(config is null)
      return;

    context.Visualizers.Remove(config);
    await context.SaveChangesAsync();

    DisposeVisualizer(visualizerId);
  }

  public async Task UpdateVisualizer(VisualizerConfig config)
  {
    await using var context = await _dbContextFactory.CreateDbContextAsync();
    var existingConfig = await context.Visualizers.FirstOrDefaultAsync(visualizer => visualizer.UniqueId == config.UniqueId);
    if(existingConfig is null)
      return;

    existingConfig.Name = config.Name;
    existingConfig.Url = config.Url;
    await context.SaveChangesAsync();

    DisposeVisualizer(existingConfig.UniqueId);

    var visualizer = await LoadExistingVisualizer(existingConfig);
    if(visualizer is null)
      return;

    _visualizerRepo.AddVisualizer(visualizer);
    _visualizerMonitors.Add(new RemoteVisualizerMonitor(visualizer, _visualizerCache));
  }

  public Task UpdateVisualizerSettings(VisualizerSettings visualizerSettings)
  {
    var visualizer = _visualizerRepo.GetVisualizerById(visualizerSettings.VisualizerId);
    if(visualizer is null)
      return Task.CompletedTask;

    visualizer.UpdateSettings(visualizerSettings.Settings);

    if(visualizer is not RemoteVisualizer remoteVisualizer)
      return Task.CompletedTask;

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
      return null;
    }

    return new RemoteVisualizer(config.Name, uri, _datamodelVersionValidator, config.UniqueId);
  }

  private async Task<RemoteVisualizer?> LoadExistingVisualizer(VisualizerConfig config)
  {
    var visualizer = ConfigToVisualizer(config);
    if(visualizer is null)
      return null;

    try
    {
      var visualizerInfo = await _visualizerCache.GetCachedVisualizerInfo(config.UniqueId);
      if(visualizerInfo is not null)
        await visualizer.UpdateInfo(visualizerInfo);

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
    }

    return visualizer;
  }

  private void DisposeVisualizer(string visualizerId)
  {
    var visualizer = _visualizerRepo.GetVisualizerById(visualizerId);
    
    if(visualizer is null)
      return;

    _visualizerRepo.RemoveVisualizer(visualizer);

    var monitor = _visualizerMonitors.FirstOrDefault(monitor => monitor.VisualizerId == visualizerId);
    if(monitor is not null)
    {
      monitor.Dispose();
      _visualizerMonitors.Remove(monitor);
    }

    if(visualizer is IDisposable disposable)
      disposable.Dispose();
  }
}