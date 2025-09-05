using Ares.Services.Device;
using FlirCM3.Services;
using Google.Protobuf.WellKnownTypes;

namespace UI.Backend.ViewModels.Devices.CM3Camera;

public class CM3CameraMultiViewModel : UsbDeviceConnectorViewModel<CM3CameraUnitControlViewModel>
{
  private readonly FlirCM3CameraRpc.FlirCM3CameraRpcClient _client;

  public CM3CameraMultiViewModel(FlirCM3CameraRpc.FlirCM3CameraRpcClient client, AresDevices.AresDevicesClient devicesClient) : base(devicesClient)
  {
    _client = client;
  }

  protected override CM3CameraUnitControlViewModel CreateUnitVm(string deviceId, string deviceName) => new(deviceId, deviceName, _client);

  protected override async Task<AresDeviceDescription[]> GetDeviceDescriptions()
  {
    var devicesResponse = await _client.GetAllCM3CamerasAsync(new Empty());
    var descriptions = devicesResponse.Cameras.Select(c => new AresDeviceDescription(c.Id, c.Name)).ToArray();
    return descriptions;
  }
}
