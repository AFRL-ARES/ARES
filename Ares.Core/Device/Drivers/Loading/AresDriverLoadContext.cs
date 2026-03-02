using System.Reflection;
using System.Runtime.Loader;

namespace Ares.Core.Device.Drivers.Loading;

public class AresDriverLoadContext : AssemblyLoadContext
{
  private readonly AssemblyDependencyResolver _resolver;

  public AresDriverLoadContext(string pluginPath) : base(isCollectible: true)
  {
    _resolver = new AssemblyDependencyResolver(pluginPath);
  }

  protected override Assembly? Load(AssemblyName assemblyName)
  {
    // This ensures that 'IAresDevice' in the driver is the EXACT same type as 'IAresDevice' in the host.
    if(assemblyName.Name != null &&
       (assemblyName.Name.StartsWith("Ares.Device") ||
        assemblyName.Name.StartsWith("Ares.Datamodel") ||
        assemblyName.Name.StartsWith("Ares.Toolkit.Device")))
    {
      return null; // Fallback to the Default Load Context (the host)
    }

    string? assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
    if(assemblyPath != null)
    {
      return LoadFromAssemblyPath(assemblyPath);
    }

    return null;
  }

  protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
  {
    string? libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
    if(libraryPath != null)
    {
      return LoadUnmanagedDllFromPath(libraryPath);
    }

    return IntPtr.Zero;
  }
}
