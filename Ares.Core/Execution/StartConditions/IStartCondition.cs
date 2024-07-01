namespace Ares.Core.Execution.StartConditions;

public interface IStartCondition
{
  /// <summary>
  /// Checks whether this start condition is satisfied
  /// </summary>
  /// <returns>True if condition is satisfied, false otherwise</returns>
  public StartConditionResult? CanStart();
}
