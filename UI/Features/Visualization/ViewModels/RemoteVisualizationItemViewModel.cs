using Ares.Core.Visualization.Providers;
using Ares.Datamodel.Visualizing.Remote;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;

namespace UI.Features.Visualization.ViewModels;

public partial class RemoteVisualizationItemViewModel : ReactiveObject, IDisposable
{
  private readonly IRemoteVisualizationDataProvider _dataProvider;
  private readonly CompositeDisposable _subscriptions = new();
  private RemoteVisualizationConfig _config;
  private readonly Action<string> _onDeleteRequested;
  private readonly Action<string, RemoteVisualizationConfig> _onUpdateRequested;

  public RemoteVisualizationItemViewModel(
    RemoteVisualizationConfig config,
    IRemoteVisualizationDataProvider dataProvider,
    Action<string> onDeleteRequested,
    Action<string, RemoteVisualizationConfig> onUpdateRequested)
  {
    _config = config;
    _dataProvider = dataProvider;
    _onDeleteRequested = onDeleteRequested;
    _onUpdateRequested = onUpdateRequested;

    UniqueId = config.UniqueId;
    Title = string.IsNullOrWhiteSpace(config.ChartTitle)
      ? config.RequestedVisual
      : config.ChartTitle;
    VisualizerId = config.VisualizerId;
    RequestedVisual = config.RequestedVisual;
    GridX = config.GridX;
    GridY = config.GridY;
    GridW = config.GridW > 0 ? config.GridW : 6;
    GridH = config.GridH > 0 ? config.GridH : 5;

    UpdateLatestVisualization();
    _dataProvider.Connect().Subscribe(_ => UpdateLatestVisualization()).DisposeWith(_subscriptions);
  }

  private void UpdateLatestVisualization()
  {
    if(string.IsNullOrWhiteSpace(VisualizerId) || string.IsNullOrWhiteSpace(RequestedVisual))
    {
      ChartJson = null;
      return;
    }

    var latest = _dataProvider.Get(VisualizerId, RequestedVisual);
    ChartJson = latest?.ChartJson;
    LastUpdated = latest?.GeneratedAt;
    VisualizerName = latest?.VisualizerName ?? VisualizerId;
  }

  public void UpdateFromConfig(RemoteVisualizationConfig newConfig)
  {
    _config = newConfig;
    Title = string.IsNullOrWhiteSpace(newConfig.ChartTitle)
      ? newConfig.RequestedVisual
      : newConfig.ChartTitle;
    VisualizerId = newConfig.VisualizerId;
    RequestedVisual = newConfig.RequestedVisual;
    GridX = newConfig.GridX;
    GridY = newConfig.GridY;
    GridW = newConfig.GridW > 0 ? newConfig.GridW : 6;
    GridH = newConfig.GridH > 0 ? newConfig.GridH : 5;
    UpdateLatestVisualization();
  }

  public void SaveSettings()
  {
    _config.ChartTitle = Title;
    _config.GridX = GridX;
    _config.GridY = GridY;
    _config.GridW = GridW;
    _config.GridH = GridH;
    _onUpdateRequested(UniqueId, _config);
  }

  public void OnDelete()
    => _onDeleteRequested(UniqueId);

  public string UniqueId { get; }

  [Reactive]
  public partial string Title { get; set; }

  [Reactive]
  public partial string VisualizerId { get; private set; }

  [Reactive]
  public partial string RequestedVisual { get; private set; }

  [Reactive]
  public partial string VisualizerName { get; private set; } = "Visualizer";

  [Reactive]
  public partial string? ChartJson { get; private set; }

  [Reactive]
  public partial DateTime? LastUpdated { get; private set; }

  public int GridX { get; set; }
  public int GridY { get; set; }
  public int GridW { get; set; }
  public int GridH { get; set; }

  public void Dispose()
  {
    _subscriptions.Dispose();
    GC.SuppressFinalize(this);
  }
}
