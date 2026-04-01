using Ares.Datamodel;
using Ares.Datamodel.Visualizing;
using Ares.Datamodel.Visualizing.Local;
using Ares.Device;
using DynamicData;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System.Reactive;
using System.Reactive.Linq;
using UI.Features.Visualization.Models;

namespace UI.Features.Visualization.ViewModels;

public partial class VisualizationItemViewModel : ReactiveObject, IDisposable
{
  private IDisposable? _streamSubscription;
  private readonly SourceList<ChartDataPoint> _dataPointsSource = new();
  private readonly IAresDevice _device;
  private readonly DeviceVisualizationConfig _config;
  private readonly Action<string> _onDeleteRequested;
  private int _pollingFrequencyMs = 250;
  private readonly object _bufferLock = new object();
  private readonly List<ChartDataPoint> _internalBuffer = new(50);
  private DateTime _lastTimestamp = DateTime.MinValue;

  public VisualizationItemViewModel(DeviceVisualizationConfig config, IAresDevice device, Action<string> onDeleteRequested)
  {
    _config = config;
    _device = device;
    _onDeleteRequested = onDeleteRequested;
    LatestDisplayValue = "Waiting for data... ";

    UniqueId = config.UniqueId ?? Guid.NewGuid().ToString();
    Title = $"{device.Name} : {config.Path.Path}";
    Style = config.Style;

    _dataPointsSource.Connect()
        .Bind(out var dataPoints)
        .Sample(TimeSpan.FromMilliseconds(500))
        .Subscribe(_ => this.RaisePropertyChanged(nameof(DataPoints)));
    DataPoints = dataPoints;

    ToggleEditCommand = ReactiveCommand.Create(() => { IsEditing = !IsEditing; });
    DeleteCommand = ReactiveCommand.Create(() => { _onDeleteRequested?.Invoke(UniqueId); });
    SaveSettingsCommand = ReactiveCommand.Create(() =>
    {
      IsEditing = false;
      // TODO: Dispatch an RPC call to update the DeviceVisualizationConfig on the ARES backend
    });

    StartStreamSubscription();
  }

  private void StartStreamSubscription()
  {
    _streamSubscription?.Dispose();

    _streamSubscription = _device.StateStream
        .Do(state => ProcessNewState(state, _config.Path))
        .Sample(TimeSpan.FromMilliseconds(500))
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
      var now = DateTime.UtcNow;
      if(now <= _lastTimestamp) now = _lastTimestamp.AddMilliseconds(1);
      _lastTimestamp = now;

      LatestNumericValue = numericValue;
      LatestDisplayValue = numericValue.ToString("0.##");

      if(Style is ChartStyle.Line or ChartStyle.Spline or ChartStyle.Area or ChartStyle.Column)
      {
        lock(_bufferLock)
        {
          _internalBuffer.Add(new ChartDataPoint(now, numericValue));

          while(_internalBuffer.Count > 50)
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

  public void Dispose()
  {
    _streamSubscription?.Dispose();
    _dataPointsSource?.Dispose();
    GC.SuppressFinalize(this);
  }

  public ReactiveCommand<Unit, Unit> ToggleEditCommand { get; }
  public ReactiveCommand<Unit, Unit> DeleteCommand { get; }
  public ReactiveCommand<Unit, Unit> SaveSettingsCommand { get; }
  public string UniqueId { get; }
  public string Title { get; }

  [Reactive]
  public partial bool IsEditing { get; set; }
  [Reactive]
  public partial ChartStyle Style { get; set; }
  [Reactive]
  public partial string LatestDisplayValue { get; set; }
  [Reactive]
  public partial double LatestNumericValue { get; set; }
  [Reactive]
  public partial IList<ChartDataPoint> DataPoints { get; set; }

  public int PollingFrequencyMs
  {
    get => _pollingFrequencyMs;
    set
    {
      if(value != _pollingFrequencyMs)
      {
        this.RaiseAndSetIfChanged(ref _pollingFrequencyMs, value);
        StartStreamSubscription();
      }
    }
  }
}
