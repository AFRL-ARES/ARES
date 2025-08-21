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

  protected override RestDeviceUnitControlViewModel CreateUnitVm(string deviceId, string deviceName) => new(deviceId, deviceName, _client);

  protected override async Task<AresDeviceDescription[]> GetDeviceDescriptions()
  {
    var devicesResponse = await _client.GetAllRestDevicesAsync(new Empty());
    var descriptions = devicesResponse.Devices.Select(dev => new AresDeviceDescription(dev.Id, dev.Name)).ToArray();
    return descriptions;
  }
}
