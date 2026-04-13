using Ares.Datamodel;
using Ares.Datamodel.Visualizing;
using Ares.Datamodel.Visualizing.Local;
using Ares.Device;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System.Reactive.Linq;
using UI.Features.Visualization.Models;

namespace UI.Features.Visualization.ViewModels;

public partial class VisualizationItemViewModel : ReactiveObject, IDisposable
{
  private IDisposable? _streamSubscription;
  private readonly IAresDevice _device;
  private DeviceVisualizationConfig _config;
  private readonly Action<string> _onDeleteRequested;
  private readonly object _bufferLock = new object();
  private readonly List<ChartDataPoint> _internalBuffer = new(50);
  private readonly Action<string, DeviceVisualizationConfig> _onUpdateRequested;

  public VisualizationItemViewModel(DeviceVisualizationConfig config, 
    IAresDevice device, 
    Action<string> onDeleteRequested, 
    Action<string, DeviceVisualizationConfig> onUpdateRequested)
  {
    _config = config;
    _device = device;
    _onDeleteRequested = onDeleteRequested;
    _onUpdateRequested = onUpdateRequested;

    PollingFrequencyMs = config.PollingRate;
    LatestDisplayValue = "Waiting for data... ";
    NumberOfDisplayPoints = config.NumberDisplayPoints;
    DisplayLabels = config.ShowDataLabels;
    DisplayMarkers = config.ShowMarkers;
    DataPoints = [];
    UniqueId = config.UniqueId;
    Title = $"{device.Name} : {config.Path.Path}";
    Style = config.Style;

    GridX = config.GridX;
    GridY = config.GridY;
    GridW = config.GridW > 0 ? config.GridW : 4;
    GridH = config.GridH > 0 ? config.GridH : 4;

    StartStreamSubscription();
  }

  public void StartStreamSubscription()
  {
    _streamSubscription?.Dispose();
    Console.WriteLine($"Disposing the old stream, creating a new one with polling frequency of {PollingFrequencyMs}");
    _streamSubscription = _device.StateStream
        .Sample(TimeSpan.FromMilliseconds(PollingFrequencyMs))
        .Do(state => ProcessNewState(state, _config.Path))
        .Subscribe(
            onNext: _ =>
            {
              lock(_bufferLock)
              {
                DataPoints = _internalBuffer.ToList();
              }
            },
            onError: ex => Console.WriteLine($"[{Title}] Stream error: {ex.Message}")
        );
  }


  private void ProcessNewState(AresStruct state, VisualizationPath path)
  {
    if(TryExtractValue(state, path, out double numericValue))
    {
      LatestNumericValue = numericValue;
      LatestDisplayValue = numericValue.ToString("0.##");

      if(Style is ChartStyle.Line or ChartStyle.Spline or ChartStyle.Area or ChartStyle.Column)
      {
        lock(_bufferLock)
        {
          _internalBuffer.Add(new ChartDataPoint(DateTime.UtcNow, numericValue));

          while(_internalBuffer.Count > NumberOfDisplayPoints)
          {
            _internalBuffer.RemoveAt(0);
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

    //Update Gridstack Position
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
    _streamSubscription?.Dispose();
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
  public partial IList<ChartDataPoint> DataPoints { get; set; }
  [Reactive]
  public partial int NumberOfDisplayPoints { get; set; }
  [Reactive]
  public partial bool DisplayLabels { get; set; }
  [Reactive]
  public partial int PollingFrequencyMs { get; set; }
  [Reactive]
  public partial bool DisplayMarkers { get; set; }
}
