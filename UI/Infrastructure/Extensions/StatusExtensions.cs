using Ares.Datamodel;

namespace UI.Infrastructure.Extensions;

public static class StatusExtensions
{
  public static bool IsActive(this ExperimentExecutionStatus experimentExecutionStatus)
  {
    return experimentExecutionStatus.StepExecutionStatuses
      .SelectMany(stepStatus => stepStatus.CommandExecutionStatuses)
      .Any(state => !(state.State == ExecutionState.Succeeded || state.State == ExecutionState.Failed));
  }

  public static bool IsActive(this CampaignExecutionState campaignExecutionState)
  {
    return campaignExecutionState.State != ExecutionState.Succeeded &&
      campaignExecutionState.State != ExecutionState.Failed;
  }

  public static bool IsPaused(this CampaignExecutionState campaignExecutionState)
  {
    return campaignExecutionState.State == ExecutionState.Paused || campaignExecutionState.State == ExecutionState.AwaitingUser;
  }

  public static bool IsPaused(this ExperimentExecutionStatus experimentExecutionStatus)
  {
    return experimentExecutionStatus.StepExecutionStatuses
      .SelectMany(stepStatus => stepStatus.CommandExecutionStatuses)
      .Any(state => state.State == ExecutionState.Paused || state.State == ExecutionState.AwaitingUser);
  }

  public static IEnumerable<CommandExecutionStatus> GetCommandExecutionStatuses(this ExperimentExecutionStatus experimentExecutionStatus)
  {
    return experimentExecutionStatus.StepExecutionStatuses.SelectMany(stepStatus => stepStatus.CommandExecutionStatuses);
  }

  public static IEnumerable<CommandExecutionStatus> GetStartupExecutionStatuses(this CampaignStartupStatus campaignStartupStatus)
  {
    return campaignStartupStatus.StartupExecutionStatuses.SelectMany(stepStatus => stepStatus.CommandExecutionStatuses);
  }

  public static IEnumerable<CommandExecutionStatus> GetStartupExecutionStatuses(this CampaignCloseoutStatus campaignCloseoutStatus)
  {
    return campaignCloseoutStatus.CloseoutExecutionStatuses.SelectMany(stepStatus => stepStatus.CommandExecutionStatuses);
  }
}