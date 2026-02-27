namespace Ares.Core.Execution.Safety;

public interface IExecutionSafetyManager
{
  Task<bool> EnterSafeMode();
}
