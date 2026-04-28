using Ares.Datamodel;
using Ares.Datamodel.Visualizing;
using Ares.Datamodel.Visualizing.Local;
using Ares.Device;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using UI.Features.Visualization.Models;

namespace UI.Features.Visualization.ViewModels;

public partial class VisualizationItemViewModel : ReactiveObject, IDisposable
{
  private readonly CompositeDisposable _streamSubscription = new();
  private readonly IEnumerable<IAresDevice> _devices;
  private DeviceVisualizationConfig _config;
  private readonly Action<string> _onDeleteRequested;
  private readonly object _bufferLock = new object();

  // NEW: Changed from a single list to a Dictionary mapped by "DeviceName - Path"
  private readonly Dictionary<string, List<ChartDataPoint>> _internalBuffers = new();

  private readonly Action<string, DeviceVisualizationConfig> _onUpdateRequested;

  public VisualizationItemViewModel(DeviceVisualizationConfig config,
    IEnumerable<IAresDevice> devices,
    Action<string> onDeleteRequested,
    Action<string, DeviceVisualizationConfig> onUpdateRequested)
  {
    _config = config;
    _devices = devices;
    _onDeleteRequested = onDeleteRequested;
    _onUpdateRequested = onUpdateRequested;

    PollingFrequencyMs = config.PollingRate;
    LatestDisplayValue = "Waiting for data... ";
    NumberOfDisplayPoints = config.NumberDisplayPoints;
    DisplayLabels = config.ShowDataLabels;
    DisplayMarkers = config.ShowMarkers;

    // Initialize the new dictionary
    DataPoints = new Dictionary<string, IList<ChartDataPoint>>();

    UniqueId = config.UniqueId;
    if(config.Paths.Count == 1 && devices.Count() == 1)
      Title = $"{devices.First().Name} : {config.Paths.First().Path}";
    else
      Title = $"Multi-Data Display Chart";

    Style = config.Style;

    GridX = config.GridX;
    GridY = config.GridY;
    GridW = config.GridW > 0 ? config.GridW : 4;
    GridH = config.GridH > 0 ? config.GridH : 4;

    StartStreamSubscription();
  }

  public void StartStreamSubscription()
  {
    _streamSubscription.Clear();

    lock(_bufferLock)
    {
      _internalBuffers.Clear();
    }

    foreach(var device in _devices)
    {
      if(device is null)
        continue;

      var matchingPaths = _config.Paths.Where(p => p.AssociatedDeviceName == device.UniqueId).ToList();
      if(!matchingPaths.Any()) continue;

      device.StateStream
        .Sample(TimeSpan.FromMilliseconds(PollingFrequencyMs))
        .Do(state => ProcessNewState(state, matchingPaths, device.Name))
        .Subscribe(
            onNext: _ =>
            {
              lock(_bufferLock)
              {
                DataPoints = _internalBuffers.ToDictionary(
                    kvp => kvp.Key,
                    kvp => (IList<ChartDataPoint>)kvp.Value.ToList()
                );
              }
            },
            onError: ex => Console.WriteLine($"[{Title}] Stream error: {ex.Message}")
        ).DisposeWith(_streamSubscription);
    }
  }

  private void ProcessNewState(AresStruct state, IEnumerable<VisualizationPath> paths, string deviceName)
  {
    foreach(var path in paths)
    {
      if(TryExtractValue(state, path, out double numericValue))
      {
        // Note: For Text/Gauge displays, this will just hold the most recently processed value.
        LatestNumericValue = numericValue;
        LatestDisplayValue = numericValue.ToString("0.##");

        if(Style is ChartStyle.Line or ChartStyle.Spline or ChartStyle.Area or ChartStyle.Column)
        {
          // NEW: Create a unique key for each line on the chart
          string seriesKey = $"{deviceName}: {path.Path}";

          lock(_bufferLock)
          {
            if(!_internalBuffers.ContainsKey(seriesKey))
            {
              _internalBuffers[seriesKey] = new List<ChartDataPoint>(NumberOfDisplayPoints + 10);
            }

            _internalBuffers[seriesKey].Add(new ChartDataPoint(DateTime.UtcNow, numericValue));

            while(_internalBuffers[seriesKey].Count > NumberOfDisplayPoints)
            {
              _internalBuffers[seriesKey].RemoveAt(0);
            }
          }
        }
      }
    }
  }

  private bool TryExtractValue(AresStruct state, VisualizationPath pathConfig, out double value)
  {
    value = 0;

    if(state == null || pathConfig == null || string.IsNullOrWhiteSpace(pathConfig.Path) || !pathConfig.IsPlottable)
      return false;

    try
    {
      string[] segments = pathConfig.Path.Split('.');
      AresStruct currentStruct = state;

      for(int i = 0; i < segments.Length - 1; i++)
      {
        string segment = segments[i];

        if(currentStruct.Fields.TryGetValue(segment, out AresValue nextValue) && nextValue.KindCase == AresValue.KindOneofCase.StructValue)
          currentStruct = nextValue.StructValue;
        else
          return false;
      }

      string leafSegment = segments[^1];

      if(currentStruct.Fields.TryGetValue(leafSegment, out AresValue leafValue))
      {
        switch(leafValue.KindCase)
        {
          case AresValue.KindOneofCase.NumberValue:
            value = leafValue.NumberValue;
            return true;

          case AresValue.KindOneofCase.QuantityValue:
            value = leafValue.QuantityValue.Scalar;
            return true;

          case AresValue.KindOneofCase.BoolValue:
            value = leafValue.BoolValue ? 1.0 : 0.0;
            return true;

          case AresValue.KindOneofCase.StringValue:
            if(double.TryParse(leafValue.StringValue, out double parsedVal))
            {
              value = parsedVal;
              return true;
            }
            break;
        }
      }
    }
    catch(Exception)
    {
      // Catch-all to ensure malformed state dictionaries don't crash the ReactiveUI stream
    }

    return false;
  }

  public void UpdateFromConfig(DeviceVisualizationConfig newConfig)
  {
    _config = newConfig;

    GridX = _config.GridX;
    GridY = _config.GridY;
    GridW = _config.GridW;
    GridH = _config.GridH;
  }

  public void SaveSettings()
  {
    _config.Style = Style;
    _config.PollingRate = PollingFrequencyMs;
    _config.ShowDataLabels = DisplayLabels;
    _config.NumberDisplayPoints = NumberOfDisplayPoints;
    _config.ShowMarkers = DisplayMarkers;
    _onUpdateRequested?.Invoke(UniqueId, _config);
  }

  public void OnDelete()
    => _onDeleteRequested?.Invoke(UniqueId);

  public void Dispose()
  {
    _streamSubscription.Dispose();
    GC.SuppressFinalize(this);
  }

  public string UniqueId { get; }
  public string Title { get; }
  public int GridX { get; set; }
  public int GridY { get; set; }
  public int GridW { get; set; }
  public int GridH { get; set; }

  [Reactive]
  public partial ChartStyle Style { get; set; }
  [Reactive]
  public partial string LatestDisplayValue { get; set; }
  [Reactive]
  public partial double LatestNumericValue { get; set; }

  [Reactive]
  public partial IDictionary<string, IList<ChartDataPoint>> DataPoints { get; set; }

  [Reactive]
  public partial int NumberOfDisplayPoints { get; set; }
  [Reactive]
  public partial bool DisplayLabels { get; set; }
  [Reactive]
  public partial int PollingFrequencyMs { get; set; }
  [Reactive]
  public partial bool DisplayMarkers { get; set; }
}