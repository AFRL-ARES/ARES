using RestSerialDevice.Generics;

namespace RestSerialDevice.Structure;

public class RestDeviceMethod
{
  public string Name { get; set; } = string.Empty;
  public string Path { get; set; } = string.Empty;
  public string Description { get; set; } = string.Empty;
  public List<RestDeviceParameter> Parameters { get; set; } = new List<RestDeviceParameter>();
  public List<RestSerialDeviceOutput> Output { get; set; } = new List<RestSerialDeviceOutput>();
}
