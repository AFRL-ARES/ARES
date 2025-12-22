using ChemyxPumpPlugin;
using ChemyxPumpPlugin.Services;
using Google.Protobuf.WellKnownTypes;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace UI.Backend.ViewModels.Devices.ChemyxPump;

public class IndividualPumpViewModel : ReactiveObject, IAsyncDisposable
{
  private readonly int _pumpNumber;
  private readonly string _deviceId;
  private readonly ChemyxPumpRpc.ChemyxPumpRpcClient _client;
  private readonly CancellationTokenSource _cts;
  private Task _listeners = Task.CompletedTask;


  public IndividualPumpViewModel(int pumpNumber, string deviceId, ChemyxPumpRpc.ChemyxPumpRpcClient client)
  {
    _pumpNumber = pumpNumber;
    _deviceId = deviceId;
    _client = client;
    _cts = new CancellationTokenSource();
  }

  public void Start()
  {
    _listeners = Task.WhenAll(
      AutoUpdateParams(_cts.Token),
      AutoUpdateState(_cts.Token)
    );
  }

  public async Task ForceUpdate()
  {
    await UpdateState();
    await RetrieveParams();
  }

  private async Task AutoUpdateState(CancellationToken token)
  {
    while(!token.IsCancellationRequested)
    {
      try
      {
        await UpdateState(token);
        await Task.Delay(TimeSpan.FromMilliseconds(500), token);
      }
      catch(Exception)
      {
        await Task.Delay(TimeSpan.FromSeconds(1), token);
      }
    }
  }

  private async Task AutoUpdateParams(CancellationToken token)
  {
    while(!token.IsCancellationRequested)
    {
      try
      {
        await RetrieveParams(token);
        await Task.Delay(TimeSpan.FromSeconds(10), token);
      }
      catch(Exception)
      {
      }
    }
  }

  public async Task UpdateState(CancellationToken? token = null)
  {
    var elapsed = await _client.GetElapsedTimeAsync(new GetElapsedTimeRequest { DeviceId = _deviceId, PumpNumber = _pumpNumber }, cancellationToken: token ?? CancellationToken.None);
    var dispensed = await _client.GetDispensedVolumeAsync(new GetDispensedVolumeRequest { DeviceId = _deviceId, PumpNumber = _pumpNumber }, cancellationToken: token ?? CancellationToken.None);
    var status = await _client.GetPumpStatusAsync(new PumpStatusRequest { DeviceId = _deviceId, PumpNumber = _pumpNumber });

    Elapsed = elapsed.ElapsedTime?.ToTimeSpan() ?? TimeSpan.Zero;
    Dispensed = dispensed.VolumeDispense;
    Status = (PumpStatus)status.PumpStatus;
  }

  public async Task RetrieveParams(CancellationToken? token = null)
  {
    var paramsRequest = new GetViewParametersRequest()
    {
      DeviceId = _deviceId
    };

    var parametersResponse = await _client.GetViewParametersAsync(paramsRequest, cancellationToken: token ?? CancellationToken.None);
    var parameters = parametersResponse.Params[_pumpNumber - 1];

    Rate = parameters.Rate;
    Volume = parameters.Volume;
    Delay = parameters.Delay.ToTimeSpan();
    Time = parameters.Time.ToTimeSpan();
    PumpUnit = parameters.Unit;
    Diameter = parameters.Diameter;
  }

  public async Task StartPump()
  {
    await _client.StartPumpAsync(new StartPumpRequest { DeviceId = _deviceId, PumpNumber = _pumpNumber });
  }

  public async Task StopPump()
  {
    await _client.StopPumpAsync(new StopPumpRequest { DeviceId = _deviceId, PumpNumber = _pumpNumber });
  }

  public async Task PausePump()
  {
    await _client.PausePumpAsync(new PausePumpRequest { DeviceId = _deviceId, PumpNumber = _pumpNumber });
  }

  public int PumpNumber => _pumpNumber;

  public async Task SetUnit(Units unit)
  {
    await _client.SetUnitsAsync(new SetUnitsRequest { DeviceId = _deviceId, PumpNumber = PumpNumber, Unit = unit }).ResponseAsync;
    await RetrieveParams();
  }

  public async Task SetRate(double rate)
  {
    await _client.SetPumpRateAsync(new SetPumpRateRequest { DeviceId = _deviceId, PumpNumber = PumpNumber, DesiredRate = rate }).ResponseAsync;
    await RetrieveParams();
  }

  public async Task SetVolume(double volume)
  {
    await _client.SetVolumeAsync(new SetVolumeRequest { DeviceId = _deviceId, PumpNumber = PumpNumber, RequestedVolume = volume }).ResponseAsync;
    await RetrieveParams();
  }

  public async Task SetDelay(TimeSpan delay)
  {
    await _client.SetDelayAsync(new SetDelayRequest { DeviceId = _deviceId, PumpNumber = PumpNumber, DesiredDelay = delay.ToDuration() }).ResponseAsync;
    await RetrieveParams();
  }

  public async Task SetTime(TimeSpan time)
  {
    await _client.SetTimeAsync(new SetTimeRequest { DeviceId = _deviceId, PumpNumber = PumpNumber, DesiredTime = time.ToDuration() }).ResponseAsync;
    await RetrieveParams();
  }

  public async Task SetDiameter(double diameter)
  {
    await _client.SetDiameterAsync(new SetDiameterRequest { DeviceId = _deviceId, PumpNumber = PumpNumber, Diameter = diameter }).ResponseAsync;
    await RetrieveParams();
  }

  [Reactive]
  public PumpStatus Status { get; set; } = PumpStatus.Stopped;

  [Reactive]
  public Units PumpUnit { get; private set; }

  [Reactive]
  public double Rate { get; private set; }

  [Reactive]
  public double Volume { get; private set; }

  [Reactive]
  public double Diameter { get; private set; }

  [Reactive]
  public double Dispensed { get; private set; }

  [Reactive]
  public TimeSpan Elapsed { get; private set; }

  [Reactive]
  public TimeSpan Delay { get; private set; }

  [Reactive]
  public TimeSpan Time { get; private set; }

  public static Units[] AvailableUnits { get; } = System.Enum.GetValues<Units>().Where(u => u != Units.Unknown).ToArray();

  public async ValueTask DisposeAsync()
  {
    await _cts.CancelAsync();
    try
    {
      await _listeners;
    }
    catch
    {
    }
  }

  public string UniqueId => $"_deviceId-{_pumpNumber}";
}
