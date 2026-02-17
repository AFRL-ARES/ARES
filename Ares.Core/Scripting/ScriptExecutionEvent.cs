using Ares.Datamodel;
using Ares.Datamodel.Scripting;

namespace Ares.Core.Scripting;

public abstract record ScriptExecutionEvent(long Sequence);

public sealed record ScriptExecutionStartedEvent(long Sequence) : ScriptExecutionEvent(Sequence);

public sealed record ScriptExecutionCompletedEvent(long Sequence) : ScriptExecutionEvent(Sequence);

public sealed record ScriptExecutionFailedEvent(long Sequence, string Error) : ScriptExecutionEvent(Sequence);

public sealed record ScriptConsoleOutputEvent(long Sequence, string Output) : ScriptExecutionEvent(Sequence);

public sealed record ScriptFunctionStartedEvent(
  long Sequence,
  string CallId,
  string ParentCallId,
  ScriptFunctionInvocation Invocation) : ScriptExecutionEvent(Sequence);

public sealed record ScriptFunctionCompletedEvent(
  long Sequence,
  string CallId,
  AresValue Result,
  string ResultText) : ScriptExecutionEvent(Sequence);

public sealed record ScriptFunctionFailedEvent(
  long Sequence,
  string CallId,
  string Error) : ScriptExecutionEvent(Sequence);
