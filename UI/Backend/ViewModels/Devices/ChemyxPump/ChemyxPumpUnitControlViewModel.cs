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

    // TODO make this work based on the DualPump parameter
    var firstPump = new IndividualPumpViewModel(1, deviceId, _client);
    var secondPump = new IndividualPumpViewModel(2, deviceId, _client);
    PumpViewModels = [firstPump, secondPump];
  }

  public IndividualPumpViewModel[] PumpViewModels { get; }
}
