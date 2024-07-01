using System.Reactive.Linq;
using System.Reactive.Subjects;
using Ares.Core.Execution.ControlTokens;
using Ares.Messaging;
using Google.Protobuf.WellKnownTypes;

namespace Ares.Core.Execution.Executors;

public class CommandExecutor : IExecutor<CommandResult, CommandExecutionStatus>
{
  private readonly Func<CancellationToken, Task<DeviceCommandResult>> _command;
  private readonly BehaviorSubject<CommandExecutionStatus> _stateSubject;

  public CommandExecutor(Func<CancellationToken, Task<DeviceCommandResult>> command, CommandTemplate template)
  {
    _command = command;
    Template = template;
    var executionStatus = new CommandExecutionStatus
    {
      CommandId = template.UniqueId,
      State = ExecutionState.Undefined
    };

    _stateSubject = new BehaviorSubject<CommandExecutionStatus>(executionStatus);
    StatusObservable = _stateSubject.AsObservable();
  }

  public CommandTemplate Template { get; set; }

  public IObservable<CommandExecutionStatus> StatusObservable { get; }
  public CommandExecutionStatus Status => _stateSubject.Value;

  public async Task<CommandResult> Execute(ExecutionControlToken token)
  {
    Status.State = token.IsPaused ? ExecutionState.Paused : ExecutionState.Running;
    _stateSubject.OnNext(Status);
    if (token.IsPaused)
      try
      {
        token.WaitForResume(token.CancellationToken);
      }
      catch (OperationCanceledException)
      {
      }

    if (token.IsCancelled)
    {
      Status.State = ExecutionState.Failed;
      _stateSubject.OnNext(Status);
      _stateSubject.OnCompleted();
      return ExecutorResultHelpers.CreateCommandResult(Template.UniqueId, null, DateTime.UtcNow, DateTime.UtcNow);
    }

    var timeStarted = DateTime.UtcNow;
    var execInfo = new ExecutionInfo { TimeStarted = DateTime.UtcNow.ToTimestamp() };
    var result = await InternalExecute(token.CancellationToken);
    execInfo.TimeFinished = DateTime.UtcNow.ToTimestamp();
    Status.State = ExecutionState.Succeeded;
    _stateSubject.OnNext(Status);
    _stateSubject.OnCompleted();
    return ExecutorResultHelpers.CreateCommandResult(Template.UniqueId, result, timeStarted, DateTime.UtcNow);
  }

  private async Task<DeviceCommandResult> InternalExecute(CancellationToken token)
  {
    try
    {
      var result = await _command(token);
      return result;
    }
    catch (Exception e)
    {
      var result = new DeviceCommandResult() { Success = false, Error = e.Message };
      return result;
    }
  }
}
