using HerkulexDRS.Services;

namespace UI.Backend.ViewModels.Devices.HerkulexDRS;

public class ServoUnitControlViewModel : SerialDeviceUnitViewModel
{
    private readonly HerkulexDRSRpc.HerkulexDRSRpcClient _client;

    public ServoUnitControlViewModel(string deviceName, HerkulexDRSRpc.HerkulexDRSRpcClient client) : base(deviceName)
    {
        _client = client;
    }

    public void PistonUp()
    {
        _client.PistonUp(new DeviceRequest { DeviceName = DeviceName });
    }

    public void PistonDown()
    {
        _client.PistonDown(new DeviceRequest { DeviceName = DeviceName });
    }

    public void ServoReset()
    {
        _client.ResetServo(new DeviceRequest { DeviceName = DeviceName });
    }
}
