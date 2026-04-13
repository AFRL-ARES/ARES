using Ares.Datamodel;

namespace AresScript.Interpreters;

public enum AresFunctionExecutionEventKind
{
  Started,
  Completed,
  Failed
}

/// <summary>
/// This describes the runtime of a function. So whenever a function starts, completed, or fails, this
/// event record gets emitted along with a result or error where appropriate. It uses the 
/// <see cref="AresFunctionInvocation"/> to describe the function.
/// </summary>
/// <param name="CallId">A unique call id for this function execution lifecycle. The id will be the same from started to completed (or error)</param>
/// <param name="ParentCallId"></param>
/// <param name="Invocation"></param>
/// <param name="Result"></param>
/// <param name="Error"></param>
public sealed record AresFunctionExecutionEvent(
  AresFunctionExecutionEventKind Kind,
  string CallId,
  string ParentCallId,
  AresFunctionInvocation Invocation,
  AresValue? Result = null,
  string? Error = null);
