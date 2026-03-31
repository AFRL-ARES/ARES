using UI.Features.Visualization.Models;
using Ares.Datamodel;
using Ares.Datamodel.Visualizing.Local;
using Ares.Device;
using DynamicData;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using Ares.Datamodel.Visualizing;

namespace UI.Features.Visualization.ViewModels;

public partial class VisualizationItemViewModel : ReactiveObject, IDisposable
{
  private readonly IDisposable _streamSubscription;
  private readonly SourceList<ChartDataPoint> _dataPointsSource = new();
  private readonly ReadOnlyObservableCollection<ChartDataPoint> _dataPoints;
  private string _latestDisplayValue = "Waiting for data...";
  private double _latestNumericValue;

  public VisualizationItemViewModel(DeviceVisualizationConfig config, IAresDevice device)
  {
    Style = config.Style;
    Title = $"{device.Name} : {config.Path.Path}";

    _dataPointsSource.Connect()
        .Bind(out _dataPoints)
        .Subscribe(_ => this.RaisePropertyChanged(nameof(DataPoints)));

    _streamSubscription = device.StateStream
        .Sample(TimeSpan.FromMilliseconds(1000))
        .Subscribe(state =>
        {
          ProcessNewState(state, config.Path);
        });

    UniqueId = Guid.NewGuid().ToString();
  }

  private void ProcessNewState(AresStruct state, VisualizationPath path)
  {
    if(TryExtractValue(state, path, out double numericValue))
    {
      var now = DateTime.UtcNow;

      LatestNumericValue = numericValue;
      LatestDisplayValue = numericValue.ToString("0.##");

      if(Style is ChartStyle.Line or ChartStyle.Spline or ChartStyle.Area or ChartStyle.Column)
      {
        _dataPointsSource.Add(new ChartDataPoint(now, numericValue));

        if(_dataPointsSource.Count > 50)
        {
          _dataPointsSource.RemoveAt(0);
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

  public ReadOnlyObservableCollection<ChartDataPoint> DataPoints => _dataPoints;
  public ChartStyle Style { get; }
  public string Title { get; }

  public string LatestDisplayValue
  {
    get => _latestDisplayValue;
    set => this.RaiseAndSetIfChanged(ref _latestDisplayValue, value);
  }

  public double LatestNumericValue
  {
    get => _latestNumericValue;
    set => this.RaiseAndSetIfChanged(ref _latestNumericValue, value);
  }

  public string UniqueId { get; set;  }
}
