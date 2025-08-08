using Ares.Datamodel;
using Ares.Datamodel.Extensions;

namespace DemoRemoteAnalyzer;

public static class DemoDataTypes
{
  public static readonly KeyValuePair<string, SchemaEntry> Operand = new KeyValuePair<string, SchemaEntry>("Operand", AresSchemaHelper.CreateSchemaEntry(AresDataType.Number, true));
  public static readonly KeyValuePair<string, SchemaEntry> InputNumber = new KeyValuePair<string, SchemaEntry>("InputNumber", AresSchemaHelper.CreateSchemaEntry(AresDataType.Number, false));
  public static readonly KeyValuePair<string, SchemaEntry> Operation = new KeyValuePair<string, SchemaEntry>(
    "Operation",
    AresSchemaHelper.CreateSchemaEntry(AresDataType.String, false, ["Multiply", "Divide"]));

  public static readonly KeyValuePair<string, SchemaEntry> RandomTags = new("RandomTags",
    AresSchemaHelper.CreateSchemaEntry(AresDataType.StringArray, true));
  public static readonly KeyValuePair<string, SchemaEntry> PreselectedTags = new("Preselected Tags",
    AresSchemaHelper.CreateSchemaEntry(AresDataType.StringArray, true, ["Tag1", "Tag2", "Tag3"]));
}
