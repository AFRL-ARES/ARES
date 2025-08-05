using Ares.Device.Serial.Commands;

namespace GenericSerialDevice.Commands.Responses;

public class GetDeviceInfoResponse : SerialResponse
{
  public GetDeviceInfoResponse(string id, string name, string hardware, bool connected)
  {
    DeviceId = id;
    DeviceName = name;
    Hardware = hardware;
    Connected = connected;
  }

  public string DeviceId { get; set; }
  public string DeviceName { get; set; }
  public string Hardware { get; set; }
  public bool Connected { get; set; }
}
