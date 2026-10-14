using Ares.Core.Device.Providers;
using Ares.Core.Visualization.Managers;
using Ares.Core.Visualization.Providers;
using Ares.Datamodel;
using Ares.Datamodel.Visualizing;
using Ares.Datamodel.Visualizing.Local;
using Ares.Datamodel.Visualizing.Remote;
using Ares.Device;
using DynamicData;
using Radzen;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using UI.Features.Visualization.Models;

namespace UI.Features.Visualization.ViewModels;

public partial class VisualizationSidebarViewModel : ReactiveObject
{
  private readonly IVisualizationConfigManager _visualizationConfigManager;
  private readonly IRemoteVisualizationConfigManager _remoteVisualizationConfigManager;
  private readonly IVisualizerProvider _visualizerProvider;
  private readonly ReadOnlyObservableCollection<IAresDevice> _devices;
  private readonly IDisposable _cleanUp;
  private readonly ILogger<VisualizationSidebarViewModel> _logger;
  private readonly NotificationService _notificationService;

  public VisualizationSidebarViewModel(
    IAresDeviceProvider deviceProvider,
    IVisualizationConfigManager visualizationConfigManager,
    IRemoteVisualizationConfigManager remoteVisualizationConfigManager,
    IVisualizerProvider visualizerProvider,
    ILogger<VisualizationSidebarViewModel> logger,
    NotificationService notificationService)
  {
    _cleanUp = deviceProvider.Connect()
        .Bind(out _devices)
        .Subscribe();

    _visualizationConfigManager = visualizationConfigManager;
    _remoteVisualizationConfigManager = remoteVisualizationConfigManager;
    _visualizerProvider = visualizerProvider;
    _notificationService = notificationService;
    _logger = logger;
    AllPaths = [];
    VisiblePaths = [];
    SelectedPaths = [];
    AvailableChartStyles = [];
    AvailableRemoteVisualizations = BuildRemoteVisualizationChoices();

    this.WhenAnyValue(x => x.BrowsingDevice, x => x.ShowAllValues)
        .Subscribe(tuple =>
        {
          var (device, showAll) = tuple;

          if(device is null)
          {
            AllPaths = [];
            VisiblePaths = [];
            return;
          }

          AllPaths = ExtractPaths(device, device.StateSchema).ToList();
          VisiblePaths = AllPaths.Where(p => showAll || p.IsPlottable).ToList();
        });

    this.WhenAnyValue(x => x.AvailableChartStyles)
        .Where(styles => styles != null && styles.Any())
        .Subscribe(styles => SelectedChartStyle = styles.First());
  }

  public void UpdateAvailableChartStyles()
  {
    if(!SelectedPaths.Any())
      return;

    switch(SelectedPaths.First().DataType)
    {
      case AresDataType.Boolean:
        AvailableChartStyles = [ChartStyle.TextIndicator, ChartStyle.Line];
        break;
      case AresDataType.String:
        AvailableChartStyles = [ChartStyle.TextIndicator, ChartStyle.Line];
        break;
      case AresDataType.Number:
      case AresDataType.Float:
      case AresDataType.Int:
        AvailableChartStyles = [ChartStyle.Line, ChartStyle.Spline, ChartStyle.Area, ChartStyle.Gauge];
        break;
      case AresDataType.Quantity:
        AvailableChartStyles = [ChartStyle.Line, ChartStyle.Spline, ChartStyle.Area, ChartStyle.Gauge];
        break;
      default:
        break;
    }
  }

  private IEnumerable<VisualizationPath> ExtractPaths(IAresDevice device, AresStructSchema stateSchema, string prefix = "")
  {
    var paths = new List<VisualizationPath>();

    foreach(var field in stateSchema.Fields)
    {
      string currentPath = string.IsNullOrEmpty(prefix) ? field.Key : $"{prefix}.{field.Key}";
      var type = field.Value.Type;

      if(type == AresDataType.Number || type == AresDataType.Float || type == AresDataType.Int || type == AresDataType.Quantity || type == AresDataType.Boolean)
        paths.Add(new VisualizationPath { Path = currentPath, DataType = type, IsPlottable = true, AssociatedDeviceId = device.UniqueId });

      else if(type == AresDataType.Struct && field.Value.StructSchema != null)
        paths.AddRange(ExtractPaths(device, field.Value.StructSchema, currentPath));

      else if(type == AresDataType.List && field.Value.ListElementSchema?.Type == AresDataType.Struct)
        paths.AddRange(ExtractPaths(device, field.Value.ListElementSchema.StructSchema!, $"{currentPath}[*]"));

      else if(type == AresDataType.String)
        paths.Add(new VisualizationPath { Path = currentPath, DataType = type, IsPlottable = false, AssociatedDeviceId = device.UniqueId });
    }

    return paths;
  }

  public async Task CreateVisualization()
  {
    try
    {
      if(SelectedPaths.Any())
        await _visualizationConfigManager.AddDeviceVisualization(SelectedPaths, SelectedChartStyle);

      SelectedPaths.Clear();
      BrowsingDevice = null;
    }

    catch(Exception e)
    {
      _logger.LogError("Error encountered inside the Visualization Side Bar View Model! {exception}", e.Message);
      _notificationService.Notify(NotificationSeverity.Error, "Failed to Create Visualization", e.Message);
    }
  }

  public async Task AddRemoteVisualization()
  {
    if(SelectedRemoteVisualization is null)
      return;

    var config = new RemoteVisualizationConfig
    {
      UniqueId = Guid.NewGuid().ToString(),
      ChartTitle = SelectedRemoteVisualization.VisualName,
      VisualizerId = SelectedRemoteVisualization.VisualizerId,
      RequestedVisual = SelectedRemoteVisualization.VisualName,
      GridW = 6,
      GridH = 5
    };

    await _remoteVisualizationConfigManager.Add(config);
    SelectedRemoteVisualization = null;
  }

  private IReadOnlyList<RemoteVisualizationChoice> BuildRemoteVisualizationChoices()
    => _visualizerProvider.GetAllVisualizers()
      .SelectMany(visualizer => visualizer.VisualizationOptions.Select(option => new RemoteVisualizationChoice(
        visualizer.UniqueId,
        visualizer.Name,
        option.VisualizationName,
        $"{visualizer.Name} - {option.VisualizationName}")))
      .OrderBy(choice => choice.VisualizerName, StringComparer.OrdinalIgnoreCase)
      .ThenBy(choice => choice.VisualName, StringComparer.OrdinalIgnoreCase)
      .ToArray();

  public void AddPath()
  {
    if(SelectedPaths.Any() && SelectedPaths.First().DataType != CurrentlySelectedPath?.DataType)
      return;

    else if(CurrentlySelectedPath is not null)
      SelectedPaths.Add(CurrentlySelectedPath);

    if(SelectedPaths.Count == 1)
      UpdateAvailableChartStyles();
  }

  public void ClearPaths()
    => SelectedPaths.Clear();

  public void RemovePath(VisualizationPath path)
    => SelectedPaths.Remove(path);

  public void Dispose()
    => _cleanUp.Dispose();

  [Reactive]
  public partial IAresDevice? BrowsingDevice { get; set; }

  [Reactive]
  public partial bool ShowAllValues { get; set; }

  [Reactive]
  public partial List<VisualizationPath> AllPaths { get; set; }

  [Reactive]
  public partial IEnumerable<VisualizationPath> VisiblePaths { get; set; }

  [Reactive]
  public partial List<VisualizationPath> SelectedPaths { get; set; }

  [Reactive]
  private ChartStyle _selectedChartStyle = ChartStyle.Line;

  [Reactive]
  private partial AresDataType SelectedDataType { get; set; }

  [Reactive]
  public partial IEnumerable<ChartStyle> AvailableChartStyles { get; set; }

  [Reactive]
  public partial VisualizationPath? CurrentlySelectedPath { get; set; }

  public ReadOnlyObservableCollection<IAresDevice> AvailableDevices => _devices;

  [Reactive]
  public partial RemoteVisualizationChoice? SelectedRemoteVisualization { get; set; }

  public IReadOnlyList<RemoteVisualizationChoice> AvailableRemoteVisualizations { get; }
}