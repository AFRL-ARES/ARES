using Ares.Services.Device;
using Google.Protobuf.WellKnownTypes;
using ValveController.Services;

namespace UI.Backend.ViewModels.Devices.ValveController;

public class ValveControllerMultiViewModel : SerialDeviceConnectorViewModel<ValveControllerUnitControlViewModel>
{
  private readonly ValveControllerRpc.ValveControllerRpcClient _client;

  public ValveControllerMultiViewModel(ValveControllerRpc.ValveControllerRpcClient client, AresDevices.AresDevicesClient devicesClient) : base(devicesClient)
  {
    _client = client;
  }

  protected override ValveControllerUnitControlViewModel CreateUnitVm(AresDeviceDescription description)
    => new(description.Id, description.Name, _client);

  protected override async Task<AresDeviceDescription[]> GetDeviceDescriptions()
  {
    var devicesResponse = await _client.GetAllValveControllersAsync(new Empty());
    var descriptions = devicesResponse.Devices.Select(d => new AresDeviceDescription(d.Id, d.Name)).ToArray();
    return descriptions;
  }
}
