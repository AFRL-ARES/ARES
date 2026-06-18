using Ares.Datamodel;

namespace Ares.Core.Execution.StopConditions;

public class NumExperimentsRun : IStopCondition
{
  private readonly IExecutionReportStore _executionReportStore;
  private readonly uint _numExperiments;

  public NumExperimentsRun(IExecutionReportStore executionReportStore, uint numExperiments)
  {
    _executionReportStore = executionReportStore;
    _numExperiments = numExperiments;
  }

  public string Message => $"Stopped because {_executionReportStore.CampaignExecutionStatus?.ExperimentExecutionStatuses.Count}/{_numExperiments} experiments have been run";
   
  public string Description => $"Campaign will stop after {_numExperiments} runs.";

  public bool ShouldStop()
  {
    var experiments = _executionReportStore.CampaignExecutionStatus?.ExperimentExecutionStatuses ?? Enumerable.Empty<ExperimentExecutionStatus>();

    //We skip one to account for the startup script summary
    var successCount = experiments
      .Skip(1)
      .Count(e =>
        e.StepExecutionStatuses?.Any() == true &&
        e.StepExecutionStatuses.All(s =>
            s.CommandExecutionStatuses != null &&
            s.CommandExecutionStatuses.All(c => c.State == Datamodel.ExecutionState.Succeeded)
        )
    );

    return successCount >= _numExperiments;
  }
}
