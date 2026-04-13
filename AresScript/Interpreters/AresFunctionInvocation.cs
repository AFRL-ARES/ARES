namespace AresScript.Interpreters;

public enum AresFunctionInvocationKind
{
  System,
  User,
  Extension,
  Lambda
}

/// <summary>
/// This describes the function that is being invoked. So stuff like id, name, kind, etc.
/// Nothing yet about the result.
/// </summary>
public sealed record AresFunctionInvocation(
  string FunctionId,
  string FunctionName,
  string Expression,
  int Line,
  int Column,
  AresFunctionInvocationKind Kind);
