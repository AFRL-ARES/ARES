using Ares.Core.Device.Plugins.Manifest;
using Ares.Core.Resources;
using Ares.Datamodel;
using Ares.Datamodel.Extensions;

namespace Ares.Core.Device.Plugins.Drivers.Loading;

public static class DriverLoaderUtils
{
  public static AresStructSchema CreateDriverSettingsSchema(List<DriverSettingDefinition> manifestSettings)
  {
    var aresSettings = new AresStructSchema();

    foreach(var definition in manifestSettings)
    {
      if(Enum.TryParse<AresDataType>(definition.Type, true, out var type))
      {
        aresSettings.AddEntry(definition.Key, type, description: definition.Description);
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
