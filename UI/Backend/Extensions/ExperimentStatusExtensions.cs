using Ares.Messaging;

namespace UI.Backend.Extensions;

public static class ExperimentStatusExtensions
{
  public static bool IsActive(this ExperimentExecutionStatus experimentExecutionStatus)
  {
    return experimentExecutionStatus.StepExecutionStatuses
      .SelectMany(stepStatus => stepStatus.CommandExecutionStatuses)
      .All(state => !(state.State == ExecutionState.Succeeded || state.State == ExecutionState.Failed));
  }

  public static bool IsPaused(this ExperimentExecutionStatus experimentExecutionStatus)
  {
    return experimentExecutionStatus.StepExecutionStatuses
      .SelectMany(stepStatus => stepStatus.CommandExecutionStatuses)
      .Any(state => state.State == ExecutionState.Paused);
  }

  public static IEnumerable<CommandExecutionStatus> GetCommandExecutionStatuses(this ExperimentExecutionStatus experimentExecutionStatus)
  {
    return experimentExecutionStatus.StepExecutionStatuses.SelectMany(stepStatus => stepStatus.CommandExecutionStatuses);
  }
}