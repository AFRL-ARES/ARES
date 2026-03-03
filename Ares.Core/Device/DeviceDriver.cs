using System.Reflection;
using Ares.Core.Device.Manifest;
using Ares.Core.Resources;
using Ares.Datamodel;

namespace Ares.Core.Device;

public class DeviceDriver
{
  public DeviceDriver(string id)
  {
    UniqueId = id;
  }

  public string UniqueId { get; }
  public DeviceDriverManifest Manifest { get; init; } = null!;
  public Assembly Assembly { get; init; } = null!;
  public Type DriverType { get; init; } = null!;
  public Type? ViewModelType { get; init; }
  public string ModulePath { get; init; } = string.Empty;
  public int DriverSize { get; init; } = 0;
  public AresStructSchema? DriverSettings { get; init; }
  public ConnectionType ConnectionType { get; init; }
}