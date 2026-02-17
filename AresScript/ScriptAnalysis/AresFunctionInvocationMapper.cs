using Ares.Datamodel.Scripting;
using AresScript.Interpreters;

namespace AresScript.ScriptAnalysis;

public static class AresFunctionInvocationMapper
{
  public static ScriptSummaryStep ToScriptSummaryStep(AresFunctionInvocation invocation, int order = 0)
  {
    return new ScriptSummaryStep
    {
      Order = order,
      FunctionId = invocation.FunctionId,
      FunctionName = invocation.FunctionName,
      Expression = invocation.Expression,
      Line = invocation.Line,
      Column = invocation.Column,
      Kind = invocation.Kind switch
      {
        AresFunctionInvocationKind.System => FunctionInvocationKind.System,
        AresFunctionInvocationKind.Extension => FunctionInvocationKind.Extension,
        AresFunctionInvocationKind.User => FunctionInvocationKind.User,
        AresFunctionInvocationKind.Lambda => FunctionInvocationKind.Lambda,
        _ => FunctionInvocationKind.Unspecified
      }
    };
  }
}
