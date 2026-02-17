using Ares.Datamodel.Scripting;
using AresScript.Interpreters;

namespace AresScript.ScriptAnalysis;

internal static class AresScriptSummaryMapper
{
  public static ScriptFunctionInvocation[] MapInternalInvocationsToProto(
    IEnumerable<AresFunctionInvocation> invocations,
    bool includeUserFunctions,
    bool includeLambdas)
  {
    return invocations
      .Where(invocation => ShouldInclude(invocation.Kind, includeUserFunctions, includeLambdas))
      .Select((invocation, index) => AresFunctionInvocationMapper.ToScriptFunctionInvocation(invocation, index + 1))
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
}
