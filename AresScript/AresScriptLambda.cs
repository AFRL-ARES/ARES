using Ares.Datamodel;
using AresScript.Generated;

namespace AresScript;

public record AresScriptLambda(
  string Name,
  IReadOnlyList<string> Parameters,
  AresLangParser.ExpressionContext Body,
  IReadOnlyDictionary<string, AresValue> Closure) : ILambdaSymbol
{
  public ScriptSymbolKind Kind => ScriptSymbolKind.Function;
  public IReadOnlyCollection<ScriptSymbolTag> Tags { get; } = [ScriptSymbolTag.UserDefined, ScriptSymbolTag.Lambda];
  public bool IsUserDefined => true;
  public bool IsLambda => true;
}
