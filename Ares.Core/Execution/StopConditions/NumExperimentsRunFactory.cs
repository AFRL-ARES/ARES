namespace Ares.Core.Execution.StopConditions;

internal class NumExperimentsRunFactory : INumExperimentsRunFactory
{
  private readonly IExecutionReportStore _executionReportStore;

  public NumExperimentsRunFactory(IExecutionReportStore executionReportStore)
  {
    _executionReportStore = executionReportStore;
  }

  public NumExperimentsRun Create(uint numExperiments)
    => new(_executionReportStore, numExperiments);
}
