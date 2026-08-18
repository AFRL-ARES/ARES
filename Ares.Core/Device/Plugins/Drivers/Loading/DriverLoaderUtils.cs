using Ares.Core.Device.Plugins.Manifest;
using Ares.Core.Resources;
using Ares.Datamodel;

namespace Ares.Core.Device.Plugins.Drivers.Loading;

public static class DriverLoaderUtils
{
  public static AresStructSchema CreateDriverSettingsSchema(List<DriverSettingDefinition> manifestSettings)
  {
    var rootSchema = new AresStructSchema();

    foreach(var definition in manifestSettings)
    {
      var valueSchema = BuildAresValueSchema(definition);
      if(valueSchema != null)
      {
        // Assuming standard Protobuf MapField generation
        rootSchema.Fields[definition.Key] = valueSchema;
      }
    }

    return rootSchema;
  }

  private static AresValueSchema? BuildAresValueSchema(DriverSettingDefinition definition)
  {
    if(!Enum.TryParse<AresDataType>(definition.Type, true, out var type))
      return null;    

    var schema = new AresValueSchema
    {
      Type = type,
      Description = definition.Description ?? string.Empty
    };

    if(type == AresDataType.Struct && definition.Fields != null)
    {
      schema.StructSchema = new AresStructSchema();

      foreach(var childDef in definition.Fields)
      {
        var childSchema = BuildAresValueSchema(childDef);
        if(childSchema != null)
        {
          schema.StructSchema.Fields[childDef.Key] = childSchema;
        }
      }
    }

    else if(type == AresDataType.List && definition.ItemSchema != null)
    {
      var itemSchema = BuildAresValueSchema(definition.ItemSchema);
      if(itemSchema != null)
      {
        schema.ListElementSchema = itemSchema;
      }
    }

    return schema;
  }

  public static ConnectionType DetermineConnectionType(string connectionType)
  {
    var parsed = Enum.TryParse<ConnectionType>(connectionType, true, out var parsedType);

    if(!parsed)
      return ConnectionType.Other;

    return parsedType;
  }
}
