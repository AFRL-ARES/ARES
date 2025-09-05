using Ares.Datamodel;
using Ares.Datamodel.Extensions;

namespace DemoRemoteDevice;

public static class DemoDataTypes
{
  public static readonly KeyValuePair<string, SchemaEntry> InputNumber = new KeyValuePair<string, SchemaEntry>("InputNumber", AresSchemaHelper.CreateSchemaEntry(AresDataType.Number, false));

  public static readonly KeyValuePair<string, SchemaEntry> OutputNumber = new KeyValuePair<string, SchemaEntry>("OutputNumber", AresSchemaHelper.CreateSchemaEntry(AresDataType.Number, false));

  public static readonly KeyValuePair<string, SchemaEntry> RandomTags = new("RandomTags",
    AresSchemaHelper.CreateSchemaEntry(AresDataType.StringArray, true));
  public static readonly KeyValuePair<string, SchemaEntry> PreselectedTags = new("Preselected Tags",
    AresSchemaHelper.CreateSchemaEntry(AresDataType.StringArray, true, ["Tag1", "Tag2", "Tag3"]));
}

public enum Commands
{
  ECHO_NUMBER
}
