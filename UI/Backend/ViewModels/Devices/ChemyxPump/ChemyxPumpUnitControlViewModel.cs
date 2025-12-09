using ChemyxPumpPlugin.Services;
using ReactiveUI.Fody.Helpers;

namespace UI.Backend.ViewModels.Devices.ChemyxPump;

public class ChemyxPumpUnitControlViewModel : SerialDeviceUnitViewModel
{
  private readonly ChemyxPumpRpc.ChemyxPumpRpcClient _client;
  public ChemyxPumpUnitControlViewModel(string deviceId, string deviceName, ChemyxPumpRpc.ChemyxPumpRpcClient client) : base(deviceId, deviceName)
  {
    _client = client;
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
    await _client.PausePumpAsync(new PausePumpRequest { DeviceId = DeviceId, PumpNumber = pumpNumber});
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
  public double PumpTwoVolume { get; set; }

  [Reactive]
  public double PumpOneDelay { get; set; }

  [Reactive]
  public double PumpTwoDelay { get; set; }

  [Reactive]
  public double PumpOneTime { get; set; }

  [Reactive]
  public double PumpTwoTime { get; set; }

  public Units[] AvailableUnits { get; private set; } = (Units[])Enum.GetValues(typeof(Units));
}
