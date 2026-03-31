using Ares.Core.Device.Providers;
using Ares.Core.Visualization.Managers;
using Ares.Datamodel;
using Ares.Datamodel.Visualizing;
using Ares.Datamodel.Visualizing.Local;
using Ares.Device;
using DynamicData;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System.Collections.ObjectModel;
using System.Reactive.Linq;

namespace UI.Features.Visualization.ViewModels;

public partial class VisualizationSidebarViewModel : ReactiveObject
{
  private readonly IVisualizationConfigManager _visualizationConfigManager;
  private readonly ReadOnlyObservableCollection<IAresDevice> _devices;
  private readonly IDisposable _cleanUp;

  public VisualizationSidebarViewModel(IAresDeviceProvider deviceProvider, IVisualizationConfigManager visualizationConfigManager)
  {
    _cleanUp = deviceProvider.Connect()
        .Bind(out _devices)
        .Subscribe();

    _visualizationConfigManager = visualizationConfigManager;
    AllPaths = [];
    VisiblePaths = [];
    AvailableChartStyles = [];

    this.WhenAnyValue(x => x.SelectedDevice, x => x.ShowAllValues)
        .Subscribe(tuple =>
        {
          var (device, showAll) = tuple;

          if(device is null)
          {
            AllPaths = [];
            VisiblePaths = [];
            return;
          }

          AllPaths = ExtractPaths(device.StateSchema).ToList();
          VisiblePaths = AllPaths.Where(p => showAll || p.IsPlottable).ToList();
        });

    this.WhenAnyValue(x => x.AvailableChartStyles)
        .Where(styles => styles != null && styles.Any())
        .Subscribe(styles => SelectedChartStyle = styles.First());
  }

  public void UpdateAvailableChartStyles()
  {
    if(SelectedPath is null)
      return;

    switch(SelectedPath.DataType)
    {
      case AresDataType.Boolean:
        AvailableChartStyles = [ChartStyle.TextIndicator, ChartStyle.Line];
        break;
      case AresDataType.String:
        AvailableChartStyles = [ChartStyle.TextIndicator, ChartStyle.Line];
        break;
      case AresDataType.Number:
        AvailableChartStyles = [ChartStyle.Line, ChartStyle.Spline, ChartStyle.Area, ChartStyle.Gauge];
        break;
      case AresDataType.Quantity:
        AvailableChartStyles = [ChartStyle.Line, ChartStyle.Spline, ChartStyle.Area, ChartStyle.Gauge];
        break;
      default:
        break;
    }
  }

  private IEnumerable<VisualizationPath> ExtractPaths(AresStructSchema schema, string prefix = "")
  {
    var paths = new List<VisualizationPath>();

    foreach(var field in schema.Fields)
    {
      string currentPath = string.IsNullOrEmpty(prefix) ? field.Key : $"{prefix}.{field.Key}";
      var type = field.Value.Type;

      if(type == AresDataType.Number || type == AresDataType.Quantity || type == AresDataType.Boolean)
      {
        paths.Add(new VisualizationPath { Path = currentPath, DataType = type, IsPlottable = true });
      }

      else if(type == AresDataType.Struct && field.Value.StructSchema != null)
      {
        paths.AddRange(ExtractPaths(field.Value.StructSchema, currentPath));
      }

      else if(type == AresDataType.List && field.Value.ListElementSchema?.Type == AresDataType.Struct)
      {
        paths.AddRange(ExtractPaths(field.Value.ListElementSchema.StructSchema!, $"{currentPath}[*]"));
      }

      else if(type == AresDataType.String)
      {
        paths.Add(new VisualizationPath { Path = currentPath, DataType = type, IsPlottable = false });
      }
    }

    return paths;
  }

  public async Task CreateVisualization()
  {
    try
    {
      if(SelectedDevice is not null && SelectedPath is not null)
        await _visualizationConfigManager.AddDeviceVisualization(SelectedDevice.UniqueId, SelectedPath, SelectedChartStyle);
    }

    catch(Exception e)
    {
      //TODO: Log and notify
    }
  }

  public void Dispose()
  {
    _cleanUp.Dispose();
  }

  [Reactive]
  public partial IAresDevice? SelectedDevice { get; set; }

  [Reactive]
  public partial bool ShowAllValues { get; set; }

  [Reactive]
  public partial List<VisualizationPath> AllPaths { get; set; }

  [Reactive]
  public partial IEnumerable<VisualizationPath> VisiblePaths { get; set; }

  [Reactive]
  public partial VisualizationPath? SelectedPath { get; set; }

  [Reactive]
  private ChartStyle _selectedChartStyle = ChartStyle.Line;

  [Reactive]
  public IEnumerable<ChartStyle> AvailableChartStyles { get; set; }

  public ReadOnlyObservableCollection<IAresDevice> AvailableDevices => _devices;
}