using Ares.Datamodel.Scripting;
using AresScript.Interpreters;

namespace AresScript.ScriptAnalysis;

public static partial class AresScriptAnalysis
{
  public static async Task<(ScriptFunctionInvocation[] Steps, Diagnostic[] Diagnostics)> BuildScriptSummaryAsync(
    string? script,
    AresScriptEnvironment environment,
    bool includeUserFunctions = false,
    bool includeLambdas = false,
    AresValidationInterpreter.ValidationMode mode = AresValidationInterpreter.ValidationMode.Strict)
  {
    var (invocations, diagnostics) = await ValidateAndCollectInvocationsAsync(
      script,
      environment,
      mode,
      traverseFunctionDeclarationBodies: false);
    var steps = AresScriptSummaryMapper.MapInternalInvocationsToProto(
      invocations,
      includeUserFunctions,
      includeLambdas);

    return (steps, diagnostics);
  }
}
