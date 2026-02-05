using System.Collections.Generic;

namespace Ares.Core.Device;

public class DeviceConfig
{
  public string Name { get; set; } = string.Empty;
  public string DriverName { get; set; } = string.Empty; // Maps to DeviceManifest.Name
  public Dictionary<string, object> Settings { get; set; } = new();
}
