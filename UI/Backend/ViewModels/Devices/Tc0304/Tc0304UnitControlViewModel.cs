using ReactiveUI.Fody.Helpers;
using Tc0304.Services;
using UnitsNet;

namespace UI.Backend.ViewModels.Tc0304;

public class Tc0304UnitControlViewModel : SerialDeviceUnitViewModel, IAsyncDisposable
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
    _stateListener = Task.Factory.StartNew(async _ =>
    {
      Thread.CurrentThread.Name = "Datalogger State Listener View Model Thread";
      while(!_stateUpdateTokenSource.Token.IsCancellationRequested)
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
    var response = await _client.GetTemperaturesAsync(new DeviceRequest { DeviceId = DeviceId });
    if(response.Temperatures is null)
      return;

    var result = response.Temperatures;
    T1Temp = result[0] is null ? null : Temperature.FromDegreesCelsius((double)result[0]!);
    T2Temp = result[1] is null ? null : Temperature.FromDegreesCelsius((double)result[1]!);
    T3Temp = result[2] is null ? null : Temperature.FromDegreesCelsius((double)result[2]!);
    T4Temp = result[3] is null ? null : Temperature.FromDegreesCelsius((double)result[3]!);
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
