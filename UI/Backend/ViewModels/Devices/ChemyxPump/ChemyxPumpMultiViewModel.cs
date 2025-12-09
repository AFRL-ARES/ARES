using Ares.Services.Device;
using ChemyxPumpPlugin.Services;
using Google.Protobuf.WellKnownTypes;

namespace UI.Backend.ViewModels.Devices.ChemyxPump;

public class ChemyxPumpMultiViewModel : SerialDeviceConnectorViewModel<ChemyxPumpUnitControlViewModel>
{
  private readonly ChemyxPumpRpc.ChemyxPumpRpcClient _client;

  public ChemyxPumpMultiViewModel(ChemyxPumpRpc.ChemyxPumpRpcClient client, AresDevices.AresDevicesClient devicesClient) : base(devicesClient)
  {
    _client = client;
  }

  protected override ChemyxPumpUnitControlViewModel CreateUnitVm(AresDeviceDescription description) => new(description.Id, description.Name, _client);

  protected override async Task<AresDeviceDescription[]> GetDeviceDescriptions()
  {
    var devicesResponse = await _client.GetAllPumpsAsync(new Empty());
    var descriptions = devicesResponse.Devices.Select(d => new AresDeviceDescription(d.Id, d.Name)).ToArray();
    return descriptions;
  }
}
