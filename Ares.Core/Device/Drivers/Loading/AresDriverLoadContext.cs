using System.Reflection;
using System.Runtime.Loader;

namespace Ares.Core.Device.Drivers.Loading;

public class AresDriverLoadContext : AssemblyLoadContext
{
  private readonly AssemblyDependencyResolver _resolver;

  public AresDriverLoadContext(string pluginPath) : base(name: "AresPluginContext", isCollectible: false)
  {
    _resolver = new AssemblyDependencyResolver(pluginPath);
  }

  protected override Assembly? Load(AssemblyName assemblyName)
  {
    // 1. "PROPER" WAY: Ask the Host first.
    // If the Host has it (System.IO.Ports, Protobuf, etc.), use the Host's version.
    try
    {
      // This prevents the "Illegal State" by ensuring we never load 
      // a second copy of a shared system assembly.
      var hostAssembly = Default.LoadFromAssemblyName(assemblyName);
      if(hostAssembly != null) return hostAssembly;
    }
    catch { /* Host doesn't have it, continue to local resolver */ }

    // 2. Fallback to the plugin's 'bin' folder for unique hardware DLLs.
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
