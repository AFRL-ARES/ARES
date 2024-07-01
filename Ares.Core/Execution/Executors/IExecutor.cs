using Ares.Core.Execution.ControlTokens;
using Google.Protobuf;

namespace Ares.Core.Execution.Executors;

public interface IExecutor<TResult, out TStatus>
  where TResult : IMessage
  where TStatus : IMessage
{
  IObservable<TStatus> StatusObservable { get; }
  TStatus Status { get; }

  Task<TResult> Execute(ExecutionControlToken executionToken);
}
