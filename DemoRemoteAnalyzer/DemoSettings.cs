using Ares.Datamodel;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Factories;

namespace DemoRemoteAnalyzer;

public static class DemoSettings
{
  public static readonly KeyValuePair<string, AresValueSchema> TemperatureMax = new("Temperature Maximum", AresSchemaBuilder.Entry(AresDataType.Float)
    .WithDefaultValue(AresValueHelper.CreateFloat(200.0))
    .Build());

  public static readonly KeyValuePair<string, AresValueSchema> TemperatureMin = new("Temperature Minimum", AresSchemaBuilder.Entry(AresDataType.Float)
    .WithDefaultValue(AresValueHelper.CreateFloat(10.0))
    .Build());

  public static readonly KeyValuePair<string, AresValueSchema> FlowRateMax = new("Flow Rate Maximum", AresSchemaBuilder.Entry(AresDataType.Float)
    .WithDefaultValue(AresValueHelper.CreateFloat(200.0))
    .Build());

  public static readonly KeyValuePair<string, AresValueSchema> FlowRateMin = new("Flow Rate Minimum", AresSchemaBuilder
    .Entry(AresDataType.Float)
    .WithDefaultValue(AresValueHelper.CreateFloat(10.0))
    .Build());
}
