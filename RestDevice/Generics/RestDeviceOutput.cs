using RestDevice.Structure;

namespace RestDevice.Generics;

public class RestDeviceOutput
{
  public RestDeviceOutput(string name, string type, string description)
  {
    Name = name;
    Type = JsonConversionHelper.DetermineType(type);
    Description = description;
  }

  public string Name { get; set; }
  public string Description { get; set; }
  public Type Type { get; set; }
  public string? Unit { get; set; }
  public string UniqueId { get; } = Guid.NewGuid().ToString();
}
