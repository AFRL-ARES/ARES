using Ares.Services.Device;
using Google.Protobuf.WellKnownTypes;
using HerkulexDRS.Services;

namespace UI.Backend.ViewModels.Devices.HerkulexDRS;

public class ServoMultiViewModel : SerialDeviceConnectorViewModel<ServoUnitControlViewModel>
{
  private readonly HerkulexDRSRpc.HerkulexDRSRpcClient _client;

  public ServoMultiViewModel(HerkulexDRSRpc.HerkulexDRSRpcClient client, AresDevices.AresDevicesClient devicesClient) : base(devicesClient)
  {
    _client = client;
  }

  protected override ServoUnitControlViewModel CreateUnitVm(string deviceName)
    => new(deviceName, _client);

  protected override async Task<IEnumerable<string>> GetDeviceNames()
  {
    var devicesResponse = await _client.GetAllServosAsync(new Empty());
    return devicesResponse.DeviceNames;
  }
}
