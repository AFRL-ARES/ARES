using Ares.Core.Execution.ControlTokens;
using Ares.Messaging;

namespace Ares.Core.Execution.Executors;

public class SequentialStepExecutor : StepExecutor
{
  public SequentialStepExecutor(StepTemplate template, CommandExecutor[] commandExecutors) : base(template, commandExecutors)
  {
  }

  public override async Task<StepResult> Execute(ExecutionControlToken token)
  {
    var startTime = DateTime.UtcNow;
    var commandResults = new List<CommandResult>();
    foreach (var command in CommandExecutors)
    {
      if (token.IsCancelled)
        break;

      var commandResult = await command.Execute(token);
      commandResults.Add(commandResult);
    }

    return ExecutorResultHelpers.CreateStepResult(Template.UniqueId, startTime, DateTime.UtcNow, commandResults);
  }
}
