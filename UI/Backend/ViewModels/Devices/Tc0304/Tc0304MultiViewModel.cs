using Ares.Messaging.Device;
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

  protected override Tc0304UnitControlViewModel CreateUnitVm(string deviceName)
    => new(deviceName, _client);

  protected override async Task<IEnumerable<string>> GetDeviceNames()
  {
    var devicesResponse = await _client.GetAllTc0304sAsync(new Empty());
    return devicesResponse.DeviceNames;
  }
}
