using Ares.Services.Device;
using Google.Protobuf.WellKnownTypes;
using Tc0304.Services;

namespace UI.Backend.ViewModels.Tc0304;

public class Tc0304MultiViewModel : SerialDeviceConnectorViewModel<Tc0304UnitControlViewModel>
{
  private readonly TC0304Rpc.TC0304RpcClient _client;

  public Tc0304MultiViewModel(TC0304Rpc.TC0304RpcClient client, AresDevices.AresDevicesClient devicesClient)
    : base(devicesClient)
  {
    _client = client;
  }

  protected override Tc0304UnitControlViewModel CreateUnitVm(AresDeviceDescription description)
    => new(description.Id, description.Name, _client);

  protected override async Task<AresDeviceDescription[]> GetDeviceDescriptions()
  {
    var devicesResponse = await _client.GetAllTc0304sAsync(new Empty());
    var descriptions = devicesResponse.Devices.Select(d =>  new AresDeviceDescription(d.Id, d.Name));
    return descriptions.ToArray();
  }
}
