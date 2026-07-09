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
    CommandResult? deviceResult,
    DateTime startTime,
    DateTime endTime)
  {
    // TODO: Handle the other potential command template command types AB 7/9/2026
    var commandExecutionSummary = new CommandExecutionSummary
    {
      UniqueId = Guid.NewGuid().ToString(),
      ExecutionInfo = MakeExecutionInfo(startTime, endTime),
      CommandId = Guid.NewGuid().ToString(),
      Result = deviceResult,
      TemplateId = template.UniqueId,
      CommandDescription = template.DeviceCommand.Metadata.Description,
      CommandName = template.DeviceCommand.Metadata.Name,
      StatusCode = deviceResult?.StatusCode ?? CommandStatusCode.StatusUnspecified
    };

    if(template.HasOutputVarName)
      commandExecutionSummary.VarName = template.OutputVarName;

    return commandExecutionSummary;
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
