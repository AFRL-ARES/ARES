using Ares.Core.Execution.VersionChecking;
using Ares.Core.Notifications;
using Ares.Core.Visualization.Repos;
using Ares.Datamodel.Analyzing;
using Ares.Datamodel.Visualizing;
using Microsoft.Extensions.Logging;

namespace Ares.Core.Visualization.Managers;

/// <summary>
/// Demo-mode implementation of <see cref="IRemoteVisualizerManager"/>.
/// This manager does not read from or write to the database and operates
/// entirely against the in-memory visualizer repository.
/// </summary>
public class DemoRemoteVisualizerManager : IRemoteVisualizerManager
{
  private readonly IVisualizerRepo _visualizerRepo;
  private readonly INotificationHandler _notificationHandler;
  private readonly IDatamodelVersionValidator _datamodelVersionValidator;
  private readonly IVisualizerCache _visualizerCache;
  private readonly List<RemoteVisualizerMonitor> _visualizerMonitors = [];
  private readonly ILogger<DemoRemoteVisualizerManager> _logger;

  public DemoRemoteVisualizerManager(
    IVisualizerRepo visualizerRepo,
    INotificationHandler notificationHandler,
    IDatamodelVersionValidator datamodelVersionValidator,
    IVisualizerCache visualizerCache,
    ILogger<DemoRemoteVisualizerManager> logger)
  {
    _visualizerRepo = visualizerRepo;
    _notificationHandler = notificationHandler;
    _datamodelVersionValidator = datamodelVersionValidator;
    _visualizerCache = visualizerCache;
    _logger = logger;
  }

  public async Task LoadVisualizers()
  {
    var existingDemo = _visualizerRepo.GetVisualizerById(DemoIds.VisualizerId);
    if(existingDemo is null)
    {
      _logger.LogInformation("No Existing Demo Visualizer, Creating New Demo Visualizer");
      await CreateDemoVisualizer("http://localhost:5028");
    }

    _logger.LogInformation("Finished loading Visualizers");
  }

  public Task CreateVisualizer(string name, string url)
  {
    var config = new VisualizerConfig { UniqueId = Guid.NewGuid().ToString(), Name = name, Url = url };
    var visualizer = ConfigToVisualizer(config);

    if(visualizer is not null)
    {
      _visualizerRepo.AddVisualizer(visualizer);
      _visualizerMonitors.Add(new RemoteVisualizerMonitor(visualizer, _visualizerCache));
      _logger.LogInformation("[Demo Remote Visualization Manager] - Successfully Added a New Visualizer: {vis}", visualizer.Name);
    }

    return Task.CompletedTask;
  }

  public Task CreateDemoVisualizer(string url)
  {
    var config = new VisualizerConfig
    {
      UniqueId = DemoIds.VisualizerId,
      Name = DemoIds.VisualizerName,
      Url = url
    };
    var visualizer = ConfigToVisualizer(config);

    if(visualizer is not null)
    {
      _visualizerMonitors.Add(new RemoteVisualizerMonitor(visualizer, _visualizerCache));
      _visualizerRepo.AddVisualizer(visualizer);
    }

    return Task.CompletedTask;
  }

  public Task RemoveVisualizer(string visualizerId)
  {
    _visualizerRepo.RemoveVisualizer(visualizerId);
    var monitor = _visualizerMonitors.FirstOrDefault(monitor => monitor.VisualizerId == visualizerId);
    if(monitor is not null)
    {
      monitor.Dispose();
      _visualizerMonitors.Remove(monitor);
      _logger.LogInformation("[Demo Remote Visualization Manager] - Successfully Removed Visualizer with ID of {id}", visualizerId);
    }

    return Task.CompletedTask;
  }

  public Task UpdateVisualizer(VisualizerConfig config)
  {
    var existing = _visualizerRepo.GetVisualizerById(config.UniqueId);
    if(existing is not null)
    {
      RemoveVisualizer(config.UniqueId);
      var updated = ConfigToVisualizer(config);
      if(updated is not null)
      {
        _visualizerRepo.AddVisualizer(updated);
        _visualizerMonitors.Add(new RemoteVisualizerMonitor(updated, _visualizerCache));
        _logger.LogInformation("[Demo Remote Visualization Manager] - Successfully Updated Visualizer: {name}", config.Name);
      }
    }

    return Task.CompletedTask;
  }

  public Task UpdateVisualizerSettings(VisualizerSettings visualizerSettings)
  {
    var visualizer = _visualizerRepo.GetVisualizerById(visualizerSettings.VisualizerId);
    if(visualizer is not null)
    {
      visualizer.UpdateSettings(visualizerSettings.Settings);
      _logger.LogInformation("[Demo Remote Visualization Manager] - Successfully Updated Visualizer Settings for {name}", visualizer.Name);
    }

    return Task.CompletedTask;
  }

  private RemoteVisualizer? ConfigToVisualizer(VisualizerConfig config)
  {
    var uriValid = Uri.TryCreate(config.Url, UriKind.Absolute, out var uri);
    if(!uriValid || uri is null)
    {
      _ = _notificationHandler.HandleNotification("Visualizer Load Error",
        $"Failed to load a remote visualizer {config.Name} because the url {config.Url} is invalid.",
        NotificationSeverityEnum.Error);

      _logger.LogError("Failed to load visualizer as the provided URL {url} was invalid.", config.Url);

      return null;
    }

    return new RemoteVisualizer(config.Name, uri, _datamodelVersionValidator, config.UniqueId);
  }
}