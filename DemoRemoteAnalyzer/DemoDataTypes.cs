using Ares.Datamodel;
using Ares.Datamodel.Factories;

namespace DemoRemoteAnalyzer;

public static class DemoDataTypes
{
  public static readonly KeyValuePair<string, AresValueSchema> Temperature = new("Temperature", AresSchemaBuilder.Entry(AresDataType.Float).Build());
  public static readonly KeyValuePair<string, AresValueSchema> ReactionStartTime = new("Reaction Start Time", AresSchemaBuilder.Entry(AresDataType.Timestamp).Build());
  public static readonly KeyValuePair<string, AresValueSchema> ReactionEndTime = new("Reaction End Time", AresSchemaBuilder.Entry(AresDataType.Timestamp).Build());
  public static readonly KeyValuePair<string, AresValueSchema> FlowRate = new("Flow Rate", AresSchemaBuilder.Entry(AresDataType.Float).Build());
}
