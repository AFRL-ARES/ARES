using HerkulexDRS.Services;
using UI.Application.Devices;

namespace UI.Features.Devices.Servo;

public class ServoUnitControlViewModel : DeviceUnitControlViewModel
{
  private readonly HerkulexDRSRpc.HerkulexDRSRpcClient _client;

  public ServoUnitControlViewModel(string deviceId, string deviceName, HerkulexDRSRpc.HerkulexDRSRpcClient client) : base(deviceId, deviceName)
  {
    _client = client;
    ViewType = typeof(ServoControlWidgetView);
    DefaultWidth = 20;
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
    _client.ResetServo(new DeviceRequest { DeviceId = DeviceId });
  }
}

