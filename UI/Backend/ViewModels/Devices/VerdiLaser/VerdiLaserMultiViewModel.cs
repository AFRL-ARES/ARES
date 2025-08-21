using Ares.Services.Device;
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

  protected override VerdiLaserUnitControlViewModel CreateUnitVm(AresDeviceDescription description)
    => new(description.Name, description.Id, _client);

  protected override async Task<AresDeviceDescription[]> GetDeviceDescriptions()
  {
    var devicesResponse = await _client.GetAllLasersAsync(new Empty());
    var descriptions = devicesResponse.Devices.Select(d => new AresDeviceDescription(d.Name, d.Id)).ToArray();
    return descriptions;
  }
}
