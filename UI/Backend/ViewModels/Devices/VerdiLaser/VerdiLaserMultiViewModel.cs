using Ares.Messaging.Device;
using Google.Protobuf.WellKnownTypes;
using VerdiV6.Services;

namespace UI.Backend.ViewModels.Devices.VerdiLaser;

public class VerdiLaserMultiViewModel : SerialDeviceConnectorViewModel<VerdiLaserUnitControlViewModel>
{
  private readonly VerdiV6Rpc.VerdiV6RpcClient _client;

  public VerdiLaserMultiViewModel(VerdiV6Rpc.VerdiV6RpcClient client, AresDevices.AresDevicesClient devicesClient) : base(devicesClient)
  {
    _client = client;
  }

  protected override VerdiLaserUnitControlViewModel CreateUnitVm(string deviceName)
    => new(deviceName, _client);

  protected override async Task<IEnumerable<string>> GetDeviceNames()
  {
    var devicesResponse = await _client.GetAllLasersAsync(new Empty());
    return devicesResponse.DeviceNames;
  }
}
