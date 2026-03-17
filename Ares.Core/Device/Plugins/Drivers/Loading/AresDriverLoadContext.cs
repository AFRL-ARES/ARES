using System.Reflection;
using System.Runtime.Loader;

namespace Ares.Core.Device.Plugins.Drivers.Loading;

public class AresDriverLoadContext : AssemblyLoadContext
{
  private readonly AssemblyDependencyResolver _resolver;

  public AresDriverLoadContext(string pluginPath) : base(name: "AresPluginContext", isCollectible: false)
  {
    _resolver = new AssemblyDependencyResolver(pluginPath);
  }

  protected override Assembly? Load(AssemblyName assemblyName)
  {
    try
    {
      var hostAssembly = Default.LoadFromAssemblyName(assemblyName);
      if(hostAssembly != null) return hostAssembly;
    }
    catch { /* Host doesn't have it, continue to local resolver */ }

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
