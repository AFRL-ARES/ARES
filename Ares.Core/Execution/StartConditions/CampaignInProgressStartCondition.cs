using Ares.Messaging;

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

  public StartConditionResult? CanStart()
  {
    var state = _executionReportStore.CampaignExecutionStatus?.State;
    if (state is null)
      return null;

    if (state != ExecutionState.Running && state != ExecutionState.Paused)
      return new StartConditionResult(true);

    return new StartConditionResult(false, $"Campaign with id {_executionReportStore.CampaignExecutionStatus?.CampaignId} is currently in progress.");
  }
}
