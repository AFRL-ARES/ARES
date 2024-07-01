using Ares.Core.Execution.StopConditions;
using Ares.Messaging;

namespace Ares.Core.Execution.Executors;

public interface ICampaignExecutor : IExecutor<CampaignResult, CampaignExecutionStatus>
{
  IList<IStopCondition> StopConditions { get; }
}
