using Ares.Services.Device;
using Chiller.Services;
using Google.Protobuf.WellKnownTypes;

namespace UI.Backend.ViewModels.Devices.LaserChiller
{
  public class LaserChillerMultiViewModel : SerialDeviceConnectorViewModel<LaserChillerUnitControlViewModel>
  {
    private readonly ChillerRpc.ChillerRpcClient _client;

    public LaserChillerMultiViewModel(ChillerRpc.ChillerRpcClient client, AresDevices.AresDevicesClient devicesClient) : base(devicesClient)
    {
      _client = client;
    }

    protected override LaserChillerUnitControlViewModel CreateUnitVm(AresDeviceDescription description) => new(description.Id, description.Name, _client);

    protected override async Task<AresDeviceDescription[]> GetDeviceDescriptions()
    {
      var devicesResponse = await _client.GetAllChillersAsync(new Empty());
      var descriptions = devicesResponse.Chillers.Select(c => new AresDeviceDescription(c.Id, c.Name)).ToArray();
      return descriptions;
    }
  }
}
