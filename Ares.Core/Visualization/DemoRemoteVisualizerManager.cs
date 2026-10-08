using Ares.Core.Execution.VersionChecking;
using Ares.Core.Notifications;
using Ares.Datamodel.Analyzing;
using Ares.Datamodel.Visualizing;

namespace Ares.Core.Visualization;

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

  public DemoRemoteVisualizerManager(
    IVisualizerRepo visualizerRepo,
    INotificationHandler notificationHandler,
    IDatamodelVersionValidator datamodelVersionValidator,
    IVisualizerCache visualizerCache)
  {
    _visualizerRepo = visualizerRepo;
    _notificationHandler = notificationHandler;
    _datamodelVersionValidator = datamodelVersionValidator;
    _visualizerCache = visualizerCache;
  }

  public async Task LoadVisualizers()
  {
    var existingDemo = _visualizerRepo.GetVisualizerById(DemoIds.VisualizerId);
    if(existingDemo is null)
      await CreateDemoVisualizer("http://localhost:5028");
  }

  public Task CreateVisualizer(string name, string url)
  {
    var config = new VisualizerConfig { UniqueId = Guid.NewGuid().ToString(), Name = name, Url = url };
    var visualizer = ConfigToVisualizer(config);

    if(visualizer is not null)
    {
      _visualizerRepo.AddVisualizer(visualizer);
      _visualizerMonitors.Add(new RemoteVisualizerMonitor(visualizer, _visualizerCache));
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
      }
    }

    return Task.CompletedTask;
  }

  public Task UpdateVisualizerSettings(VisualizerSettings visualizerSettings)
  {
    var visualizer = _visualizerRepo.GetVisualizerById(visualizerSettings.VisualizerId);
    if(visualizer is not null)
      visualizer.UpdateSettings(visualizerSettings.Settings);

    return Task.CompletedTask;
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
}
