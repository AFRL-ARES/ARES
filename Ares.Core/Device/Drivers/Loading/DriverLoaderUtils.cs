using System.Reflection;

namespace Ares.Core.Device.Drivers.Loading;

public static class DriverLoaderUtils
{
  public static Assembly LoadDriver(string dllPath)
  {
    var fullPath = Path.GetFullPath(dllPath);
    var context = new AresDriverLoadContext(fullPath);

    return context.LoadFromAssemblyPath(fullPath);
  }
}
