using AresScript.ScriptAnalysis;
using Microsoft.JSInterop;
using UI.Application.Scripting;

namespace UI.Infrastructure.Monaco.Interops;

public sealed class MonacoSemanticTokensProvider : IMonacoSemanticTokensProvider
{
  [JSInvokable]
  public SemanticToken[] GetSemanticTokens(string script)
  {
    return AresScriptAnalysis.BuildSemanticTokens(script)
      .Select(token => new SemanticToken(
        token.Line,
        token.StartColumn,
        token.Length,
        MapTokenType(token.Type)))
      .ToArray();
  }

  private static string MapTokenType(ScriptSemanticTokenType type) => type switch
  {
    ScriptSemanticTokenType.Function => "function",
    _ => "variable"
  };
}
