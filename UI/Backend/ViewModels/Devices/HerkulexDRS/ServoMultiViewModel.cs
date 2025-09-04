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

  protected override ServoUnitControlViewModel CreateUnitVm(AresDeviceDescription description)
    => new(description.Id, description.Name, _client);

  protected override async Task<AresDeviceDescription[]> GetDeviceDescriptions()
  {
    var devicesResponse = await _client.GetAllServosAsync(new Empty());
    var descriptions = devicesResponse.Devices.Select(d => new AresDeviceDescription(d.Id, d.Name)).ToArray();
    return descriptions;
  }
}
