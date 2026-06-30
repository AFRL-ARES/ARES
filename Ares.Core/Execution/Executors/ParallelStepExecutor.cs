using Ares.Core.Execution.ControlTokens;
using Ares.Datamodel;

namespace Ares.Core.Execution.Executors;

public class ParallelStepExecutor : StepExecutor
{
  public ParallelStepExecutor(IEnumerable<IExecutor<CommandExecutionSummary, CommandExecutionStatus>> executionNodes) : base(template, executionNodes)
  {
  }

  public override async Task<StepExecutionSummary> Execute(ExecutionControlToken token)
    => await Execute(token, new Dictionary<string, AresValue>());

  public override async Task<StepExecutionSummary> Execute(ExecutionControlToken token, IReadOnlyDictionary<string, AresValue> variableScope)
  {
    var startTime = DateTime.UtcNow;
    var commandTasks = CommandExecutors.Select(command => command.Execute(token, variableScope));
    var commandSummaries = await Task.WhenAll(commandTasks);

    return ExecutorSummaryHelpers.CreateStepExecutionSummary(startTime, DateTime.UtcNow, commandSummaries);
  }
}
