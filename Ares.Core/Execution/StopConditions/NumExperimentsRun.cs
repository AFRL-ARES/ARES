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
    //Subtract one to offset the startup script, as even when it's empty it's always considered present here
    var currentExperiments = _executionReportStore.CampaignExecutionStatus?.ExperimentExecutionStatuses.Count - 1;
    return currentExperiments >= _numExperiments;
  }
}
