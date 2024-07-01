using System.Diagnostics;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Tc0304.Services;
using UnitsNet;

namespace TC0304.Blazor.ViewModels;

public class Tc0304ControlWidgetViewModel : ReactiveObject, IDisposable
{
  private readonly TC0304Rpc.TC0304RpcClient _client;
  private readonly CancellationTokenSource _stateUpdateTokenSource = new();
  private bool _stateUpdaterActive;

  public Tc0304ControlWidgetViewModel(string name, TC0304Rpc.TC0304RpcClient client)
  {
    Name = name;
    _client = client;
    StartStateUpdater();
  }

  public string Name { get; }

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

  public void Dispose()
  {
    StopStateUpdater();
    _stateUpdateTokenSource.Dispose();
  }

  private void StartStateUpdater()
  {
    Task.Factory.StartNew(async _ => {
        try
        {
          while (!_stateUpdateTokenSource.Token.IsCancellationRequested)
          {
            await UpdateState();
            await Task.Delay(TimeSpan.FromMilliseconds(500));
          }

          Trace.WriteLine("Finished stateupdated");
        }
        catch (ObjectDisposedException)
        {
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
    var response = await _client.GetDataAsync(new DeviceRequest { DeviceName = Name });
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
    _client.Hold(new DeviceRequest { DeviceName = Name });
  }
}
