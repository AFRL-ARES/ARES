using ReactiveUI.Fody.Helpers;
using Tc0304.Services;
using UnitsNet;

namespace UI.Backend.ViewModels.Tc0304;

public class Tc0304UnitControlViewModel : DeviceUnitControlViewModel, IAsyncDisposable
{
  private readonly TC0304Rpc.TC0304RpcClient _client;
  private readonly CancellationTokenSource _stateUpdateTokenSource = new();
  private Task _stateListener = Task.CompletedTask;

  public Tc0304UnitControlViewModel(string id, string name, TC0304Rpc.TC0304RpcClient client) : base(id, name)
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
    var token = _stateUpdateTokenSource.Token;

    _stateListener = Task.Run(async () =>
    {
      while (!token.IsCancellationRequested)
      {
        await UpdateState();
        await Task.Delay(TimeSpan.FromMilliseconds(500), token);
      }
    }, token);
  }

  private void StopStateUpdater()
  {
    _stateUpdateTokenSource.Cancel();
  }

  private async Task UpdateState()
  {
    var response = await _client.GetTemperaturesAsync(new DeviceRequest { DeviceId = DeviceId });

    T1Temp = response.HasProbe1C ? Temperature.FromDegreesCelsius(response.Probe1C) : null;
    T2Temp = response.HasProbe2C ? Temperature.FromDegreesCelsius(response.Probe2C) : null;
    T3Temp = response.HasProbe3C ? Temperature.FromDegreesCelsius(response.Probe3C) : null;
    T4Temp = response.HasProbe4C ? Temperature.FromDegreesCelsius(response.Probe4C) : null;
  }

  public void Hold()
  {
    _client.Hold(new DeviceRequest { DeviceId = DeviceId });
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
