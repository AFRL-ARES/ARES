using Ares.Core.Device.Manifest;
using Ares.Core.Resources;
using Ares.Datamodel;
using Ares.Datamodel.Extensions;
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

  public static AresStructSchema CreateDriverSettingsSchema(List<DriverSettingDefinition> manifestSettings)
  {
    var aresSettings = new AresStructSchema();

    foreach(var definition in manifestSettings)
    {
      if(Enum.TryParse<AresDataType>(definition.Type, true, out var type))
      {
        aresSettings.AddEntry(definition.DisplayName, type, description: definition.Description);
      }
    }

    return aresSettings;
  }

  public static ConnectionType DetermineConnectionType(string connectionType)
  {
    var parsed = Enum.TryParse<ConnectionType>(connectionType, true, out var parsedType);

    if(!parsed)
      return ConnectionType.Other;

    return parsedType;
  }
}
