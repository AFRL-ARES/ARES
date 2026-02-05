using System.Reflection;
using Ares.Core.Device.Loaders;

namespace Ares.Core.Device;

public class DeviceDriver
{
  public DeviceManifest Manifest { get; init; } = null!;
  public Assembly Assembly { get; init; } = null!;
  public Type DriverType { get; init; } = null!;
  public Type? ViewModelType { get; init; }
  public string ModulePath { get; init; } = string.Empty;
}