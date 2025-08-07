using Ares.Core.Execution.ControlTokens;
using Ares.Datamodel;
using Ares.Datamodel.Templates;

namespace Ares.Core.Execution.Executors;

public class ParallelStepExecutor : StepExecutor
{
  public ParallelStepExecutor(StepTemplate template, CommandExecutor[] commandExecutors) : base(template, commandExecutors)
  {
  }

  public override async Task<StepExecutionSummary> Execute(ExecutionControlToken token)
  {
    var startTime = DateTime.UtcNow;
    var commandTasks = CommandExecutors.Select(command => command.Execute(token));
    var commandSummaries = await Task.WhenAll(commandTasks);

    return ExecutorSummaryHelpers.CreateStepExecutionSummary(startTime, DateTime.UtcNow, commandSummaries);
  }
}
