using ChemyxPumpPlugin.Services;
using UI.Pages.Shared.Devices.ChemyxPump;

namespace UI.Backend.ViewModels.Devices.ChemyxPump;

public class ChemyxPumpUnitControlViewModel : DeviceUnitControlViewModel
{
  private readonly ChemyxPumpRpc.ChemyxPumpRpcClient _client;
  public ChemyxPumpUnitControlViewModel(string deviceId, string deviceName, ChemyxPumpRpc.ChemyxPumpRpcClient client) : base(deviceId, deviceName)
  {
    _client = client;

    // TODO make this work based on the DualPump parameter
    var firstPump = new IndividualPumpViewModel(1, deviceId, _client);
    var secondPump = new IndividualPumpViewModel(2, deviceId, _client);
    PumpViewModels = [firstPump, secondPump];
    ViewType = typeof(ChemyxPumpUnitControl);
    DefaultWidth = 40;
  }

  public IndividualPumpViewModel[] PumpViewModels { get; }
}
