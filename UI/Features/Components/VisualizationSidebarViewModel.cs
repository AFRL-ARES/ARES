using Ares.Core.Device.Providers;
using Ares.Datamodel;
using Ares.Device;
using DynamicData;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using UI.Features.Visualization;

namespace UI.Features.Components;


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

  [Reactive]
  public partial IAresDevice? SelectedDevice { get; set; }

  [Reactive]
  public partial bool ShowAllValues { get; set; }

  [Reactive]
  public partial List<VisualizationPath> AllPaths { get; set; }

  [Reactive]
  public partial IEnumerable<VisualizationPath> VisiblePaths { get; set; }

  public ReadOnlyObservableCollection<IAresDevice> AvailableDevices => _devices;
}