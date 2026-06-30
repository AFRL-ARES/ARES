using Ares.Core.Execution.ControlTokens;
using Ares.Datamodel;
using Google.Protobuf;

namespace Ares.Core.Execution.Executors;

public interface IExecutor<TResult, out TStatus>
  where TResult : IMessage
  where TStatus : IMessage
{
  IObservable<TStatus> ExperimentStatusObservable { get; }
  TStatus Status { get; }
  Task<TResult> Execute(ExecutionControlToken executionToken, IReadOnlyDictionary<string, AresValue> variableScope);
}
