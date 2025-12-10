using ChemyxPumpPlugin.Services;
using Humanizer;
using ReactiveUI.Fody.Helpers;

namespace UI.Backend.ViewModels.Devices.ChemyxPump;

public class ChemyxPumpUnitControlViewModel : SerialDeviceUnitViewModel
{
  private readonly ChemyxPumpRpc.ChemyxPumpRpcClient _client;
  public ChemyxPumpUnitControlViewModel(string deviceId, string deviceName, ChemyxPumpRpc.ChemyxPumpRpcClient client) : base(deviceId, deviceName)
  {
    _client = client;
  }

  public void StartStateRetriever()
  {
    Task.Run(async () =>
    {
      while(true)
      {
        await RetrieveState();
        var elapsed1 = await _client.GetElapsedTimeAsync(new GetElapsedTimeRequest { DeviceId = DeviceId, PumpNumber = 1 });
        var elapsed2 = await _client.GetElapsedTimeAsync(new GetElapsedTimeRequest { DeviceId = DeviceId, PumpNumber = 2 });
        var dispensed1 = await _client.GetDispensedVolumeAsync(new GetDispensedVolumeRequest { DeviceId = DeviceId, PumpNumber = 1 });
        var dispensed2 = await _client.GetDispensedVolumeAsync(new GetDispensedVolumeRequest { DeviceId = DeviceId, PumpNumber = 2 });

        PumpOneElapsedMinutes = elapsed1.ElapsedTime.Minutes().TotalMinutes;
        PumpTwoElapsedMinutes = elapsed2.ElapsedTime.Minutes().TotalMinutes;

        PumpOneDispensed = dispensed1.VolumeDispense;
        PumpTwoDispensed = dispensed2.VolumeDispense;

        await Task.Delay(750);
      }
    });
  }

  public async Task RetrieveState()
  {
    var paramsRequest = new GetViewParametersRequest()
    {
      DeviceId = DeviceId
    };

    var parametersResponse = _client.GetViewParameters(paramsRequest);
    PumpOneParams = parametersResponse.Params[0];
    if(parametersResponse.Params.Count > 1)
    {
      PumpTwoParams = parametersResponse.Params[1];
    }
  }

  public async Task StartPump(int pumpNumber)
  {
    await _client.StartPumpAsync(new StartPumpRequest { DeviceId = DeviceId, PumpNumber = pumpNumber });
  }

  public async Task StopPump(int pumpNumber)
  {
    await _client.StopPumpAsync(new StopPumpRequest { DeviceId = DeviceId, PumpNumber = pumpNumber });
  }

  public async Task PausePump(int pumpNumber)
  {
    await _client.PausePumpAsync(new PausePumpRequest { DeviceId = DeviceId, PumpNumber = pumpNumber });
  }

  public async Task PumpOneUnitUpdated()
  {
    await _client.SetUnitsAsync(new SetUnitsRequest { DeviceId = DeviceId, PumpNumber = 1, Unit = PumpOneSelectedUnit });
  }

  public async Task PumpTwoUnitUpdated()
  {
    await _client.SetUnitsAsync(new SetUnitsRequest { DeviceId = DeviceId, PumpNumber = 2, Unit = PumpTwoSelectedUnit });
  }

  [Reactive]
  public PumpParams? PumpOneParams { get; set; }

  [Reactive]
  public PumpParams? PumpTwoParams { get; set; }

  [Reactive]
  public Units PumpOneSelectedUnit { get; set; }

  [Reactive]
  public Units PumpTwoSelectedUnit { get; set; }

  [Reactive]
  public double PumpOneRate { get; set; }

  [Reactive]
  public double PumpTwoRate { get; set; }

  [Reactive]
  public double PumpOneVolume { get; set; }

  [Reactive]
  public double PumpOneDispensed { get; set; }

  [Reactive]
  public double PumpOneElapsedMinutes { get; set; }

  [Reactive]
  public double PumpTwoVolume { get; set; }

  [Reactive]
  public double PumpOneDelay { get; set; }

  [Reactive]
  public double PumpTwoDelay { get; set; }

  [Reactive]
  public double PumpOneTime { get; set; }

  [Reactive]
  public double PumpTwoTime { get; set; }

  [Reactive]
  public double PumpTwoDispensed { get; set; }

  [Reactive]
  public double PumpTwoElapsedMinutes { get; set; }

  public Units[] AvailableUnits { get; private set; } = (Units[])Enum.GetValues(typeof(Units));
}
