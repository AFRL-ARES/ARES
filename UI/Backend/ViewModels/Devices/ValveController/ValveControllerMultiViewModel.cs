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

  protected override ValveControllerUnitControlViewModel CreateUnitVm(string deviceName)
    => new(deviceName, _client);

  protected override async Task<IEnumerable<string>> GetDeviceNames()
  {
    var devicesResponse = await _client.GetAllValveControllersAsync(new Empty());
    return devicesResponse.DeviceNames;
  }
}
