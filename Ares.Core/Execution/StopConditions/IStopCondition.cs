namespace Ares.Core.Execution.StopConditions;

public interface IStopCondition
{
  public string Message { get; }
  public string Description { get; }
  public bool ShouldStop();
}
