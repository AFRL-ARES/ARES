using Ares.Datamodel;

namespace Ares.Core.Execution.StartConditions;

/// <summary>
/// A simple condition that checks whether or not the experiment is currently running
/// </summary>
internal class CampaignInProgressStartCondition : IStartCondition
{
  private readonly IExecutionReportStore _executionReportStore;

  public CampaignInProgressStartCondition(IExecutionReportStore executionReportStore)
  {
    _executionReportStore = executionReportStore;
  }

  public Task<StartConditionResult> CanStart()
  {
    var state = _executionReportStore.CampaignExecutionStatus?.State;
    if(state is null)
      // No campaign execution status means nothing has been run yet.
      return Task.FromResult(new StartConditionResult(true));

    if(state != ExecutionState.Running && state != ExecutionState.Paused)
      return Task.FromResult(new StartConditionResult(true));

    return Task.FromResult(new StartConditionResult(false, $"Campaign with id {_executionReportStore.CampaignExecutionStatus?.CampaignId} is currently in progress."));
  }
}
