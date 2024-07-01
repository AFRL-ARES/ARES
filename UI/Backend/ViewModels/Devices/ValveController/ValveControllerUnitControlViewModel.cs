using ValveController.Services;

namespace UI.Backend.ViewModels.Devices.ValveController;

public class ValveControllerUnitControlViewModel : SerialDeviceUnitViewModel
{
  private readonly ValveControllerRpc.ValveControllerRpcClient _client;

  public ValveControllerUnitControlViewModel(string deviceName, ValveControllerRpc.ValveControllerRpcClient client) : base(deviceName)
  {
    _client = client;
  }

  public void EngageRelay()
  {
    if (RelaySelected == true)
    {
      EngageRelayOne();
    }

    else
    {
      EngageRelayTwo();
    }
  }

  public void DisengageRelay()
  {
    if (RelaySelected == true)
    {
      DisengageRelayOne();
    }

    else
    {
      DisengageRelayTwo();
    }
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

  //True when relay one is selected, false when relay two is
  public bool RelaySelected { get; set; } = true;
}
