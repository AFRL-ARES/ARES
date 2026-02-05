using Ares.Core.Resources;
using System.Collections.Generic;

namespace Ares.Core.Device.Loaders;

public class DeviceManifest
{
  public string Name { get; set; } = string.Empty;
  public ConnectionType ConnectionType { get; set; }
  public string? DriverClass { get; set; }
  public string? ViewModelClass { get; set; }
  public Dictionary<string, object> Settings { get; set; } = new();
}