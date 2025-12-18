using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using TubeFurnace.Messaging;
using UI.Pages.Shared.Devices.TubeFurnace;
using UnitsNet;
using UnitsNet.Units;

namespace UI.Backend.ViewModels.TubeFurnace;

public class TubeFurnaceViewModel : DeviceUnitControlViewModel, IAsyncDisposable
{
  private readonly TubeFurnaceRpc.TubeFurnaceRpcClient _tubeFurnaceClient;
  private Task _stateListener = Task.CompletedTask;
  private CancellationTokenSource _stateUpdateTokenSource;
  private TemperatureUnit _temperatureUnit = TemperatureUnit.DegreeCelsius;

  public TubeFurnaceViewModel(string id, string deviceName, TubeFurnaceRpc.TubeFurnaceRpcClient tubeFurnaceClient) : base(id, deviceName)
  {
    _tubeFurnaceClient = tubeFurnaceClient;
    _stateUpdateTokenSource = new();
    StartStateUpdater();

    ViewType = typeof(TubeFurnaceControl);
    DefaultWidth = 18;
  }

  private void StartStateUpdater()
  {
    _stateListener = Task.Factory.StartNew(async _ =>
    {
      Thread.CurrentThread.Name = "Tube Furnace State Listener View Model Thread";
      var cancelled = false;
      lock (_stateUpdateTokenSource) { cancelled = _stateUpdateTokenSource.IsCancellationRequested; }
      while (!cancelled)
      {
        await UpdateState();
        await Task.Delay(TimeSpan.FromMilliseconds(500));
        lock (_stateUpdateTokenSource) { cancelled = _stateUpdateTokenSource.IsCancellationRequested; }
      }
    },
      _stateUpdateTokenSource.Token,
      TaskCreationOptions.LongRunning);
  }

  private async Task UpdateState()
  {
    var deviceRequest = new TubeFurnaceRequest { TubeFurnaceId = DeviceId };
    await _tubeFurnaceClient.GetSetpointAsync(deviceRequest);
    await _tubeFurnaceClient.GetCurrentTemperatureAsync(deviceRequest);

    var state = await _tubeFurnaceClient.GetStateAsync(new TubeFurnaceRequest { TubeFurnaceId = DeviceId });

    // TODO
    CurrentTemperatureValue = Temperature.FromDegreesCelsius(state.CurrentTemperature).As(TemperatureUnit);
    TargetTemperatureValue = Temperature.FromDegreesCelsius(state.Setpoint).As(TemperatureUnit);
  }

  public async ValueTask DisposeAsync()
  {
    _stateUpdateTokenSource.Cancel();
    await _stateListener;
    _stateListener.Dispose();
    _stateUpdateTokenSource.Dispose();
    GC.SuppressFinalize(this);
  }

  public async Task SetTargetTemperature(double value)
  {
    await _tubeFurnaceClient
      .SetSetpointAsync(
      new SetSetpointRequest
      {
        DeviceRequest = new TubeFurnaceRequest
        {
          TubeFurnaceId = DeviceId
        },
        DegreesCelsius = Temperature.From(value, TemperatureUnit).DegreesCelsius
      });
  }

  public TemperatureUnit TemperatureUnit
  {
    get => _temperatureUnit;
    set
    {
      TargetTemperatureValue = TargetTemperatureValue.HasValue ? Temperature.From(TargetTemperatureValue.Value, TemperatureUnit).As(value) : null;
      CurrentTemperatureValue = CurrentTemperatureValue.HasValue ? Temperature.From(CurrentTemperatureValue.Value, TemperatureUnit).As(value) : null;
      this.RaiseAndSetIfChanged(ref _temperatureUnit, value);
    }
  }
  [Reactive]
  public double? CurrentTemperatureValue { get; private set; }
  [Reactive]
  public double? TargetTemperatureValue { get; set; }
  [Reactive]
  public DurationUnit RampRateDurationUnit { get; set; }
  [Reactive]
  public double? TargetRampRateTemperatureValue { get; set; }
  [Reactive]
  public double? CurrentRampRateTemperatureValue { get; set; }
}
