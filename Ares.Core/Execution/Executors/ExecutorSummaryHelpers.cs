using Ares.Datamodel;
using Ares.Datamodel.Templates;
using Google.Protobuf.WellKnownTypes;

namespace Ares.Core.Execution.Executors;

internal static class ExecutorSummaryHelpers
{
  public static ExperimentExecutionSummary CreateExperimentExecutionSummary(ExperimentOverview experimentOverview,
    DateTime startTime,
    DateTime endTime,
    IEnumerable<StepExecutionSummary> StepSummaries)
  {
    var experimentSummary = new ExperimentExecutionSummary
    {
      UniqueId = Guid.NewGuid().ToString(),
      ExecutionInfo = MakeExecutionInfo(startTime, endTime),
      ExperimentId = experimentOverview.Template.UniqueId,
      ExperimentOverview = experimentOverview,
    };

    experimentSummary.StepSummaries.AddRange(StepSummaries);
    return experimentSummary;
  }

  public static StepExecutionSummary CreateStepExecutionSummary(DateTime startTime,
    DateTime endTime,
    IEnumerable<CommandExecutionSummary> CommandSummaries)
  {
    var stepResult = new StepExecutionSummary
    {
      UniqueId = Guid.NewGuid().ToString(),
      ExecutionInfo = MakeExecutionInfo(startTime, endTime),
      StepId = Guid.NewGuid().ToString()
    };

    stepResult.CommandSummaries.AddRange(CommandSummaries);

    return stepResult;
  }

  public static StepExecutionSummary CreateEmptyStepExecutionSummary(DateTime startTime, DateTime endTime)
  {
    return new StepExecutionSummary { UniqueId = Guid.NewGuid().ToString(), ExecutionInfo = MakeExecutionInfo(startTime, endTime) };
  }
  public static CommandExecutionSummary CreateCommandExecutionSummary(CommandTemplate template,
    DeviceCommandResult? deviceResult,
    DateTime startTime,
    DateTime endTime)
  {
    var CommandExecutionSummary = new CommandExecutionSummary
    {
      UniqueId = Guid.NewGuid().ToString(),
      ExecutionInfo = MakeExecutionInfo(startTime, endTime),
      CommandId = Guid.NewGuid().ToString(),
      Result = deviceResult,
      CommandDescription = template.Metadata.Description,
      CommandName = template.Metadata.Name
    };

    return CommandExecutionSummary;
  }

  private static ExecutionInfo MakeExecutionInfo(DateTime startTime, DateTime endTime)
    => new()
    {
      UniqueId = Guid.NewGuid().ToString(),
      TimeFinished = endTime.ToTimestamp(),
      TimeStarted = startTime.ToTimestamp(),
      Timezone = TimeZoneInfo.Local.DisplayName,
      LocaltimeOffset = DateTimeOffset.Now.Offset.ToString()
    };
}
