using Ares.Datamodel;

namespace AresScript.Interpreters;

public enum AresFunctionExecutionEventKind
{
  Started,
  Completed,
  Failed
}

public sealed record AresFunctionExecutionEvent(
  AresFunctionExecutionEventKind Kind,
  string CallId,
  string ParentCallId,
  AresFunctionInvocation Invocation,
  AresValue? Result = null,
  string? Error = null);
