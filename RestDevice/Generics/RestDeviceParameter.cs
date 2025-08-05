namespace RestDevice.Structure;

public class RestDeviceParameter
{
  public RestDeviceParameter(string name, string type)
  {
    Name = name;
    Type = JsonConversionHelper.DetermineType(type);
  }

  public string Name { get; set; }
  public Type Type { get; set; }
  public float? Minimum { get; set; }
  public float? Maximum { get; set; }
  public string? Unit { get; set; }
  public string UniqueId { get; } = Guid.NewGuid().ToString();
}
