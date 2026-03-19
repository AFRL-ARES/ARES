namespace AresScript.Interpreters;

public enum AresFunctionInvocationKind
{
  System,
  User,
  Extension,
  Lambda
}

public sealed record AresFunctionInvocation(
  string FunctionId,
  string FunctionName,
  string Expression,
  int Line,
  int Column,
  AresFunctionInvocationKind Kind);
