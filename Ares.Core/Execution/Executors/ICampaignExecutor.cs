using Ares.Core.Execution.StopConditions;
using Ares.Datamodel;

namespace Ares.Core.Execution.Executors;

public interface ICampaignExecutor : IExecutor<CampaignExecutionSummary, CampaignExecutionStatus>
{
  IList<IStopCondition> StopConditions { get; }
  double ReplanRate { get; set; }
  void UpdateExecutionNotes(string executionNotes);

  void UpdateCampaignTags(List<AresCampaignTag> campaignTags);
}
