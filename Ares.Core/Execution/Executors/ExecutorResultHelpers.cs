using Ares.Messaging;
using Google.Protobuf.WellKnownTypes;

namespace Ares.Core.Execution.Executors;

internal static class ExecutorResultHelpers
{
  public static ExperimentResult CreateExperimentResult(string experimentId,
    CompletedExperiment completedExperiment,
    DateTime startTime,
    DateTime endTime,
    IEnumerable<StepResult> stepResults)
  {
    var experimentResult = new ExperimentResult
    {
      UniqueId = Guid.NewGuid().ToString(),
      ExecutionInfo = MakeExecutionInfo(startTime, endTime),
      ExperimentId = experimentId,
      CompletedExperiment = completedExperiment
    };

    experimentResult.StepResults.AddRange(stepResults);
    return experimentResult;
  }

  public static StepResult CreateStepResult(string stepId,
    DateTime startTime,
    DateTime endTime,
    IEnumerable<CommandResult> commandResults)
  {
    var stepResult = new StepResult
    {
      UniqueId = Guid.NewGuid().ToString(),
      ExecutionInfo = MakeExecutionInfo(startTime, endTime),
      StepId = stepId
    };

    stepResult.CommandResults.AddRange(commandResults);

    return stepResult;
  }

  public static CommandResult CreateCommandResult(string commandId,
    DeviceCommandResult? deviceResult,
    DateTime startTime,
    DateTime endTime)
  {
    var commandResult = new CommandResult
    {
      UniqueId = Guid.NewGuid().ToString(),
      ExecutionInfo = MakeExecutionInfo(startTime, endTime),
      CommandId = commandId,
      Result = deviceResult
    };

    return commandResult;
  }

  private static ExecutionInfo MakeExecutionInfo(DateTime startTime, DateTime endTime)
    => new()
    {
      UniqueId = Guid.NewGuid().ToString(),
      TimeFinished = endTime.ToTimestamp(),
      TimeStarted = startTime.ToTimestamp()
    };
}
