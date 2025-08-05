using RestDevice.Generics;

namespace RestDevice.Structure;

public class RestDeviceMethod
{
  public string Name { get; set; } = string.Empty;
  public string Path { get; set; } = string.Empty;
  public string Description { get; set; } = string.Empty;
  public List<RestDeviceParameter> Parameters { get; set; } = new List<RestDeviceParameter>();
  public List<RestDeviceOutput> Output { get; set; } = new List<RestDeviceOutput>();
  public string UniqueId { get; } = Guid.NewGuid().ToString();
}
