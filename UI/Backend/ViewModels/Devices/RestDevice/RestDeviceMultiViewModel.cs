using Ares.Services.Device;
using Google.Protobuf.WellKnownTypes;
using RestDevice.Services;

namespace UI.Backend.ViewModels.Devices.RestDevice;

public class RestDeviceMultiViewModel : UsbDeviceConnectorViewModel<RestDeviceUnitControlViewModel>
{
  private readonly RestDeviceRpc.RestDeviceRpcClient _client;

  public RestDeviceMultiViewModel(RestDeviceRpc.RestDeviceRpcClient client, AresDevices.AresDevicesClient devicesClient) : base(devicesClient)
  {
    _client = client;
  }

  protected override RestDeviceUnitControlViewModel CreateUnitVm(string deviceName) => new(deviceName, _client);

  protected override async Task<IEnumerable<string>> GetDeviceNames()
  {
    var devicesResponse = await _client.GetAllRestDevicesAsync(new Empty());
    return devicesResponse.DeviceNames;
  }

  protected override async Task<IEnumerable<string>> GetDeviceIds()
  {
    var devicesResponse = await _client.GetAllRestDevicesAsync(new Empty());
    return devicesResponse.DeviceNames;
  }
}
