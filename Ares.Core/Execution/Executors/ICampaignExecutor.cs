using Ares.Core.Execution.StopConditions;
using Ares.Datamodel;
using AresScript;

namespace Ares.Core.Execution.Executors;

public interface ICampaignExecutor
{
  IList<IStopCondition> StopConditions { get; }
  double ReplanRate { get; set; }
  void UpdateExecutionNotes(string executionNotes);

  void UpdateCampaignTags(List<AresCampaignTag> campaignTags);
  Task<CampaignExecutionSummary> Execute(ScriptExecutionControlToken token);
}
