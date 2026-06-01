using Ares.Core.Execution.StopConditions;
using Ares.Datamodel;

namespace Ares.Core.Execution.Executors;

public interface ICampaignExecutor : IExecutor<CampaignExecutionSummary, CampaignExecutionStatus>
{
  IList<IStopCondition> StopConditions { get; }
  int ReplicateRate { get; set; }
  int BatchPlanningSize { get; set; }
  void UpdateExecutionNotes(string executionNotes);
  void UpdateCampaignTags(List<AresCampaignTag> campaignTags);
}
