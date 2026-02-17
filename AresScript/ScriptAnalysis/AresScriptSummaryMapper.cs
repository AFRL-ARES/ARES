using AresScript.Interpreters;
using Ares.Datamodel.Scripting;

namespace AresScript.ScriptAnalysis;

internal static class AresScriptSummaryMapper
{
  public static ScriptSummaryStep[] MapInvocationsToSummarySteps(
    IEnumerable<AresFunctionInvocation> invocations,
    bool includeUserFunctions,
    bool includeLambdas)
  {
    return invocations
      .Where(invocation => ShouldInclude(invocation.Kind, includeUserFunctions, includeLambdas))
      .Select((invocation, index) => new ScriptSummaryStep
      {
        Order = index + 1,
        FunctionId = invocation.FunctionId,
        FunctionName = invocation.FunctionName,
        Expression = invocation.Expression,
        Line = invocation.Line,
        Column = invocation.Column,
        Kind = MapKind(invocation.Kind)
      })
      .ToArray();
  }

  private static bool ShouldInclude(
    AresFunctionInvocationKind kind,
    bool includeUserFunctions,
    bool includeLambdas)
  {
    return kind switch
    {
      AresFunctionInvocationKind.System => true,
      AresFunctionInvocationKind.Extension => true,
      AresFunctionInvocationKind.User => includeUserFunctions,
      AresFunctionInvocationKind.Lambda => includeLambdas,
      _ => false
    };
  }

  private static FunctionInvocationKind MapKind(AresFunctionInvocationKind kind)
  {
    return kind switch
    {
      AresFunctionInvocationKind.System => FunctionInvocationKind.System,
      AresFunctionInvocationKind.Extension => FunctionInvocationKind.Extension,
      AresFunctionInvocationKind.User => FunctionInvocationKind.User,
      AresFunctionInvocationKind.Lambda => FunctionInvocationKind.Lambda,
      _ => FunctionInvocationKind.Unspecified
    };
  }
}
