using System.Reactive.Linq;
using Ares.Core.Execution.ControlTokens;
using Ares.Messaging;

namespace Ares.Core.Execution.Executors;

public abstract class StepExecutor : IExecutor<StepResult, StepExecutionStatus>
{
  public StepExecutor(StepTemplate template, IExecutor<CommandResult, CommandExecutionStatus>[] commandExecutors)
  {
    Template = template;
    CommandExecutors = commandExecutors;
    Status = new StepExecutionStatus
    {
      StepId = template.UniqueId
    };

    Status.CommandExecutionStatuses.AddRange(commandExecutors.Select(executor => executor.Status));

    var commandExecutionObservation = commandExecutors.Select(executor => {
      return executor.StatusObservable.Select(_ => {
        var cmdResults = commandExecutors.Select(cmdExecutor => cmdExecutor.Status);
        Status.CommandExecutionStatuses.Clear();
        Status.CommandExecutionStatuses.AddRange(cmdResults);
        return Status;
      });
    }).Concat();

    StatusObservable = commandExecutionObservation;
  }

  public IExecutor<CommandResult, CommandExecutionStatus>[] CommandExecutors { get; }
  protected StepTemplate Template { get; }

  public IObservable<StepExecutionStatus> StatusObservable { get; }
  public StepExecutionStatus Status { get; }
  public abstract Task<StepResult> Execute(ExecutionControlToken token);
}
