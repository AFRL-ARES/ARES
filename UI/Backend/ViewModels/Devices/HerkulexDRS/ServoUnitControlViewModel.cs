using HerkulexDRS.Services;

namespace UI.Backend.ViewModels.Devices.HerkulexDRS;

public class ServoUnitControlViewModel : SerialDeviceUnitViewModel
{
  private readonly HerkulexDRSRpc.HerkulexDRSRpcClient _client;

  public ServoUnitControlViewModel(string deviceId, string deviceName, HerkulexDRSRpc.HerkulexDRSRpcClient client) : base(deviceId, deviceName)
  {
    _client = client;
  }

  public void PistonUp()
  {
    _client.PistonUp(new DeviceRequest { DeviceId = DeviceId });
  }

  public void PistonDown()
  {
    _client.PistonDown(new DeviceRequest { DeviceId = DeviceId });
  }

  public void ServoReset()
  {
    _client.ResetServo(new DeviceRequest { DeviceId = DeviceName });
  }
}
