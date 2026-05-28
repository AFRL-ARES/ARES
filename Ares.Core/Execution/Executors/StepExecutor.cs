using Ares.Core.Execution.ControlTokens;
using Ares.Datamodel;
using Ares.Datamodel.Templates;
using System.Reactive.Linq;

namespace Ares.Core.Execution.Executors;

public abstract class StepExecutor : IExecutor<StepExecutionSummary, StepExecutionStatus>
{
  public StepExecutor(StepTemplate template, CommandExecutor[] commandExecutors)
  {
    Template = template;
    CommandExecutors = commandExecutors;
    Status = new StepExecutionStatus
    {
      StepId = template.UniqueId,
      StepName = template.Name
    };

    Status.CommandExecutionStatuses.AddRange(commandExecutors.Select(executor => executor.Status));

    var commandExecutionObservation = commandExecutors.Select(executor =>
    {
      return executor.ExperimentStatusObservable.Select(_ =>
      {
        var cmdResults = commandExecutors.Select(cmdExecutor => cmdExecutor.Status);
        Status.CommandExecutionStatuses.Clear();
        Status.CommandExecutionStatuses.AddRange(cmdResults);
        return Status;
      });
    }).Concat();

    ExperimentStatusObservable = commandExecutionObservation;
  }

  public CommandExecutor[] CommandExecutors { get; }
  protected StepTemplate Template { get; }
  public IObservable<StepExecutionStatus> ExperimentStatusObservable { get; }
  public StepExecutionStatus Status { get; }
  public abstract Task<StepExecutionSummary> Execute(ExecutionControlToken token);
  public virtual Task<StepExecutionSummary> Execute(ExecutionControlToken token, IReadOnlyDictionary<string, AresValue> variableScope)
    => Execute(token);
}
