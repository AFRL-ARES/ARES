using Ares.Core.Device.Providers;
using Ares.Datamodel;
using Ares.Device;
using DynamicData;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;

namespace UI.Features.Visualization.ViewModels;

public partial class VisualizationSidebarViewModel : ReactiveObject
{
  private readonly ReadOnlyObservableCollection<IAresDevice> _devices;
  private readonly IDisposable _cleanUp;

  public VisualizationSidebarViewModel(IAresDeviceProvider deviceProvider)
  {
    _cleanUp = deviceProvider.Connect()
        .Bind(out _devices)
        .Subscribe();

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

    this.WhenAnyValue(x => x.SelectedPath)
        .Select(path =>
        {
          if(path == null) 
            return Array.Empty<ChartStyle>();

          return path.DataType switch
          {
            AresDataType.Number => new[] { ChartStyle.Line, ChartStyle.Spline, ChartStyle.Area, ChartStyle.Gauge },
            AresDataType.Boolean => new[] { ChartStyle.TextIndicator, ChartStyle.Line },
            AresDataType.String => new[] { ChartStyle.TextIndicator },
            _ => new[] { ChartStyle.TextIndicator }
          };
        })
        .ToProperty(this, x => x.AvailableChartStyles);

    this.WhenAnyValue(x => x.AvailableChartStyles)
        .Where(styles => styles != null && styles.Any())
        .Subscribe(styles => SelectedChartStyle = styles.First());

    var canAdd = this.WhenAnyValue(x => x.SelectedDevice, x => x.SelectedPath,
        (dev, path) => dev != null && path != null);

    AddToDashboardCommand = ReactiveCommand.Create(() => new ChartCreationRequest
    {
      Device = SelectedDevice!,
      Path = SelectedPath!,
      SelectedStyle = SelectedChartStyle
    }, canAdd);
  }

  private IEnumerable<VisualizationPath> ExtractPaths(AresStructSchema schema, string prefix = "")
  {
    var paths = new List<VisualizationPath>();

    foreach(var field in schema.Fields)
    {
      string currentPath = string.IsNullOrEmpty(prefix) ? field.Key : $"{prefix}.{field.Key}";
      var type = field.Value.Type;

      if(type == AresDataType.Number || type == AresDataType.String || type == AresDataType.Boolean)
      {
        paths.Add(new VisualizationPath { Path = currentPath, DataType = type });
      }

      else if(type == AresDataType.Struct && field.Value.StructSchema != null)
      {
        paths.AddRange(ExtractPaths(field.Value.StructSchema, currentPath));
      }

      else if(type == AresDataType.List && field.Value.ListElementSchema?.Type == AresDataType.Struct)
      {
        paths.AddRange(ExtractPaths(field.Value.ListElementSchema.StructSchema!, $"{currentPath}[*]"));
      }

      else if(type == AresDataType.Quantity)
      {
        paths.Add(new VisualizationPath { Path = currentPath, DataType = type });
      }
    }

    return paths;
  }

  public void Dispose()
  {
    _cleanUp.Dispose();
  }

  public ReactiveCommand<Unit, ChartCreationRequest> AddToDashboardCommand { get; }

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