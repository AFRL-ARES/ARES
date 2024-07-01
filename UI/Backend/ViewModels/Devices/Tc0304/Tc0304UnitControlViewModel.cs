using ReactiveUI.Fody.Helpers;
using Tc0304.Services;
using UnitsNet;

namespace UI.Backend.ViewModels.Tc0304;

public class Tc0304UnitControlViewModel : SerialDeviceUnitViewModel, IAsyncDisposable
{
  private readonly TC0304Rpc.TC0304RpcClient _client;
  private readonly CancellationTokenSource _stateUpdateTokenSource = new();
  private Task _stateListener = Task.CompletedTask;

  public Tc0304UnitControlViewModel(string name, TC0304Rpc.TC0304RpcClient client) : base(name)
  {
    _client = client;
    StartStateUpdater();
  }

  [Reactive]
  public bool BatteryLow { get; private set; }

  [Reactive]
  public bool HoldActive { get; private set; }

  [Reactive]
  public Temperature? T1Temp { get; private set; }

  [Reactive]
  public Temperature? T2Temp { get; private set; }

  [Reactive]
  public Temperature? T3Temp { get; private set; }

  [Reactive]
  public Temperature? T4Temp { get; private set; }

  public string Probe1Name { get; set; } = "Probe 1";
  public string Probe2Name { get; set; } = "Probe 2";
  public string Probe3Name { get; set; } = "Probe 3";
  public string Probe4Name { get; set; } = "Probe 4";



  private void StartStateUpdater()
  {
    _stateListener = Task.Factory.StartNew(async _ =>
    {
      while (!_stateUpdateTokenSource.Token.IsCancellationRequested)
      {
        await UpdateState();
        await Task.Delay(TimeSpan.FromMilliseconds(500));
      }
    },
      _stateUpdateTokenSource.Token,
      TaskCreationOptions.LongRunning);
  }

  private void StopStateUpdater()
  {
    _stateUpdateTokenSource.Cancel();
  }

  private async Task UpdateState()
  {
    var response = await _client.GetDataAsync(new DeviceRequest { DeviceName = DeviceName });
    if (response.Data is null)
      return;

    var result = response.Data;
    T1Temp = result.T1Probe is null ? null : Temperature.FromDegreesCelsius(result.T1Probe.Value);
    T2Temp = result.T2Probe is null ? null : Temperature.FromDegreesCelsius(result.T2Probe.Value);
    T3Temp = result.T3Probe is null ? null : Temperature.FromDegreesCelsius(result.T3Probe.Value);
    T4Temp = result.T4Probe is null ? null : Temperature.FromDegreesCelsius(result.T4Probe.Value);
    HoldActive = result.Hold;
    BatteryLow = result.BatteryLow;
  }

  public void Hold()
  {
    _client.Hold(new DeviceRequest { DeviceName = DeviceName });
  }

  public async ValueTask DisposeAsync()
  {
    StopStateUpdater();
    await _stateListener;
    _stateListener.Dispose();
    _stateUpdateTokenSource.Dispose();
    GC.SuppressFinalize(this);
  }
}
