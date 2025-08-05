using Ares.Device.Serial.Commands;
using RestSerialDevice.Structure;

namespace RestSerialDevice.Commands.Responses;

public class GetDeviceCapabilitiesResponse : SerialResponse
{
  public GetDeviceCapabilitiesResponse(string name, string version, List<RestDeviceVariable> variables, List<RestDeviceMethod> methods)
  {
    DeviceName = name;
    FirmwareVersion = version;
    Variables = variables;
    Methods = methods;
  }

  public string DeviceName { get; set; }
  public string FirmwareVersion { get; set; }
  public List<RestDeviceVariable> Variables { get; set; }
  public List<RestDeviceMethod> Methods { get; set; }
}
