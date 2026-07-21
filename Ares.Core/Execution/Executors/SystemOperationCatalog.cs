using Ares.Datamodel;
using Ares.Datamodel.Factories;
using Ares.Datamodel.Templates;

namespace Ares.Core.Execution.Executors;

public sealed record SystemOperationDefinition(
  SystemOperation Operation,
  string DisplayName,
  string Description,
  IReadOnlyList<ParameterMetadata> Parameters,
  AresValueSchema? OutputSchema);

public static class SystemOperationCatalog
{
  private static readonly IReadOnlyList<SystemOperationDefinition> _definitions =
  [
    CreateSleepDefinition(
      SystemOperation.SleepForMilliseconds,
      "Sleep For Milliseconds",
      "Pause execution for a specified number of milliseconds."),
    CreateSleepDefinition(
      SystemOperation.SleepForSeconds,
      "Sleep For Seconds",
      "Pause execution for a specified number of seconds."),
    CreateSleepDefinition(
      SystemOperation.SleepForMinutes,
      "Sleep For Minutes",
      "Pause execution for a specified number of minutes."),
    new SystemOperationDefinition(
      SystemOperation.WaitForUser,
      "Wait For User",
      "Pause execution until the user resumes the experiment.",
      [],
      null),
    new SystemOperationDefinition(
      SystemOperation.WaitForUserInput,
      "Wait For User Input",
      "Pause execution until the user resumes the experiment.",
      [],
      null),
    new SystemOperationDefinition(
      SystemOperation.GetTimestamp,
      "Get Timestamp",
      "Return the current UTC timestamp.",
      [],
      AresSchemaBuilder.TimestampEntry()
        .WithDescription("The current UTC timestamp.")
        .Build()),
    new SystemOperationDefinition(
      SystemOperation.CalculateAverage,
      "Calculate Average",
      "Calculate the average of a list of numeric values.",
      [CreateParameter("Data", AresDataType.NumberArray)],
      AresSchemaBuilder.NumberEntry()
        .WithDescription("The average of the supplied values.")
        .Build())
  ];

  public static IReadOnlyList<SystemOperationDefinition> Definitions => _definitions;

  public static SystemOperationDefinition? Find(SystemOperation operation)
    => _definitions.FirstOrDefault(definition => definition.Operation == operation);

  private static SystemOperationDefinition CreateSleepDefinition(
    SystemOperation operation,
    string displayName,
    string description)
    => new(
      operation,
      displayName,
      description,
      [CreateParameter("Duration", AresDataType.Number)],
      null);

  private static ParameterMetadata CreateParameter(string name, AresDataType type)
    => new()
    {
      UniqueId = Guid.NewGuid().ToString(),
      Name = name,
      NotPlannable = true,
      Schema = AresSchemaBuilder.Entry(type).Build()
    };
}
