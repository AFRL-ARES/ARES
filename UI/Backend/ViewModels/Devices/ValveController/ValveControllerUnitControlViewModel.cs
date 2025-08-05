using ValveController.Services;

namespace UI.Backend.ViewModels.Devices.ValveController;

public class ValveControllerUnitControlViewModel : SerialDeviceUnitViewModel
{
  private readonly ValveControllerRpc.ValveControllerRpcClient _client;

  public ValveControllerUnitControlViewModel(string deviceName, ValveControllerRpc.ValveControllerRpcClient client) : base(deviceName)
  {
    _client = client;
  }

  public void EngageRelayOne()
  {
    _client.EngageRelayOne(new DeviceRequest { DeviceName = DeviceName });
  }

  public void DisengageRelayOne()
  {
    _client.DisengageRelayOne(new DeviceRequest { DeviceName = DeviceName });
  }

  public void EngageRelayTwo()
  {
    _client.EngageRelayTwo(new DeviceRequest { DeviceName = DeviceName });
  }

  public void DisengageRelayTwo()
  {
    _client.DisengageRelayTwo(new DeviceRequest { DeviceName = DeviceName });
  }
}
