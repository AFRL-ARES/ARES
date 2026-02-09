using UI.Backend.ViewModels;
using ValveController.Services;

namespace UI.Features.Devices.ValveController;

public class ValveControllerUnitControlViewModel : DeviceUnitControlViewModel
{
  private readonly ValveControllerRpc.ValveControllerRpcClient _client;

  public ValveControllerUnitControlViewModel(string deviceId, string deviceName, ValveControllerRpc.ValveControllerRpcClient client) : base(deviceId, deviceName)
  {
    _client = client;
    ViewType = typeof(ValveControllerWidgetView);
  }

  public void EngageRelayOne()
  {
    _client.EngageRelayOne(new DeviceRequest { DeviceId = DeviceId });
  }

  public void DisengageRelayOne()
  {
    _client.DisengageRelayOne(new DeviceRequest { DeviceId = DeviceId });
  }

  public void EngageRelayTwo()
  {
    _client.EngageRelayTwo(new DeviceRequest { DeviceId = DeviceId });
  }

  public void DisengageRelayTwo()
  {
    _client.DisengageRelayTwo(new DeviceRequest { DeviceId = DeviceId });
  }
}
