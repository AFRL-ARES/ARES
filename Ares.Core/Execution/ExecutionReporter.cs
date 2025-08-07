using Ares.Datamodel;

namespace Ares.Core.Execution;

internal class ExecutionReporter : IExecutionReporter
{
  private readonly IExecutionReportStore _reportStore;

  public ExecutionReporter(IExecutionReportStore reportStore)
  {
    _reportStore = reportStore;
  }

  public void Report(CampaignExecutionStatus status)
  {
    _reportStore.CampaignExecutionStatus = status;
  }

  public void Report(ExperimentExecutionStatus status)
  {
    _reportStore.ExperimentExecutionStatus = status;
  }

  public void Report(CampaignStartupStatus status)
  {
    _reportStore.CampaignStartupStatus = status;
  }

  public void Report(CampaignCloseoutStatus status)
  {
    _reportStore.CampaignCloseoutStatus = status;
  }
}
