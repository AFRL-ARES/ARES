namespace Ares.Core.Execution.StopConditions;

public interface INumExperimentsRunFactory
{
  public NumExperimentsRun Create(uint numExperiments);
}
