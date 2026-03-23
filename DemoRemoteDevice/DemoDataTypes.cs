using Ares.Datamodel;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Factories;

namespace DemoRemoteDevice;

public static class DemoDataTypes
{
  public static readonly KeyValuePair<string, AresValueSchema> InputNumber = new("InputNumber", AresSchemaBuilder.Entry(AresDataType.Number).Build());

  public static readonly KeyValuePair<string, AresValueSchema> OutputNumber = new("OutputNumber", AresSchemaBuilder.Entry(AresDataType.Number).Build());

  public static readonly KeyValuePair<string, AresValueSchema> RandomTags = new("RandomTags",
    AresSchemaBuilder.Entry(AresDataType.StringArray).AsOptional().Build());
  public static readonly KeyValuePair<string, AresValueSchema> PreselectedTags = new("Preselected Tags",
    AresSchemaBuilder.Entry(AresDataType.StringArray).AsOptional().WithChoices("Tag1", "Tag2", "Tag3").Build());
}

public enum Commands
{
  ECHO_NUMBER
}
