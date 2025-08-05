namespace RestSerialDevice.Structure;

public class RestDeviceVariable
{
  public RestDeviceVariable(string name, string description, string path, string type)
  {
    Name = name;
    Description = description;
    Path = path;
    Type = JsonConversionHelper.DetermineType(type);
  }

  public string Name { get; set; }

  public string Description { get; set; }

  public string Path { get; set; }

  public Type Type { get; set; }

  public string? Unit { get; set; }

  public float? Uncertainty { get; set; }

  public bool Readable { get; set; }

  public bool Writable { get; set; }
}
