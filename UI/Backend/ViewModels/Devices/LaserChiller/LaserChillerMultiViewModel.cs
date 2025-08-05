using Ares.Messaging.Device;
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

    protected override LaserChillerUnitControlViewModel CreateUnitVm(string deviceName) => new(deviceName, _client);

    protected override async Task<IEnumerable<string>> GetDeviceNames()
    {
      var devicesResponse = await _client.GetAllChillersAsync(new Empty());
      return devicesResponse.DeviceNames;
    }
  }
}
