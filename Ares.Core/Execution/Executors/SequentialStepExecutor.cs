using Ares.Core.Execution.ControlTokens;
using Ares.Messaging;

namespace Ares.Core.Execution.Executors;

public class SequentialStepExecutor : StepExecutor
{
  public SequentialStepExecutor(StepTemplate template, CommandExecutor[] commandExecutors) : base(template, commandExecutors)
  {
  }

  public override async Task<StepExecutionSummary> Execute(ExecutionControlToken token)
  {
    var startTime = DateTime.UtcNow;
    var commandSummaries = new List<CommandExecutionSummary>();
    foreach (var command in CommandExecutors)
    {
      if(token.IsCancelled)
        break;

      var commandExecutionSummary = await command.Execute(token);

      if(commandExecutionSummary.Result.Success)
        commandSummaries.Add(commandExecutionSummary);

      else
        return ExecutorSummaryHelpers.CreateEmptyStepExecutionSummary(startTime, DateTime.UtcNow);
    }

    return ExecutorSummaryHelpers.CreateStepExecutionSummary(startTime, DateTime.UtcNow, commandSummaries);
  }
}
