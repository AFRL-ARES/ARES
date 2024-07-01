using Ares.Core.Execution.ControlTokens;
using Ares.Messaging;

namespace Ares.Core.Execution.Executors;

public class ParallelStepExecutor : StepExecutor
{
  public ParallelStepExecutor(StepTemplate template, CommandExecutor[] commandExecutors) : base(template, commandExecutors)
  {
  }

  public override async Task<StepResult> Execute(ExecutionControlToken token)
  {
    var startTime = DateTime.UtcNow;
    var commandTasks = CommandExecutors.Select(command => command.Execute(token));
    var commandResults = await Task.WhenAll(commandTasks);

    return ExecutorResultHelpers.CreateStepResult(Template.UniqueId, startTime, DateTime.UtcNow, commandResults);
  }
}
