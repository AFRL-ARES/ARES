using Ares.Core.Execution.ControlTokens;
using Ares.Datamodel;
using Ares.Datamodel.Templates;

namespace Ares.Core.Execution.Executors;

public class SequentialStepExecutor : StepExecutor
{
  public SequentialStepExecutor(StepTemplate template, CommandExecutor[] commandExecutors) : base(template, commandExecutors)
  {
  }

  public override async Task<StepExecutionSummary> Execute(ExecutionControlToken token)
    => await Execute(token, new Dictionary<string, AresValue>());

  public override async Task<StepExecutionSummary> Execute(ExecutionControlToken token, IReadOnlyDictionary<string, AresValue> variableScope)
  {
    var startTime = DateTime.UtcNow;
    var commandSummaries = new List<CommandExecutionSummary>();
    var combinedScope = new Dictionary<string, AresValue>(variableScope);
    foreach (var command in CommandExecutors)
    {
      if(token.IsCancelled)
        break;

      var commandExecutionSummary = await command.Execute(token, combinedScope);

      if(commandExecutionSummary.Result.Success)
      {
        commandSummaries.Add(commandExecutionSummary);

        foreach(var variable in CommandVariableResolver.CreateVariableScope([commandExecutionSummary]))
          combinedScope[variable.Key] = variable.Value;
      }

      else
        return ExecutorSummaryHelpers.CreateEmptyStepExecutionSummary(startTime, DateTime.UtcNow);
    }

    return ExecutorSummaryHelpers.CreateStepExecutionSummary(startTime, DateTime.UtcNow, commandSummaries);
  }
}
