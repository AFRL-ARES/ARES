using Ares.Datamodel;
using AresScript.Generated;

namespace AresScript;

public record AresScriptParameter(string Name, AresDataType Type = AresDataType.Any);

public record AresScriptFunction(
  string Name,
  IReadOnlyList<AresScriptParameter> Parameters,
  AresLangParser.FuncBlockContext Body,
  AresDataType ReturnType = AresDataType.Any) : IFunctionSymbol
{
  public ScriptSymbolKind Kind => ScriptSymbolKind.Function;
  public IReadOnlyCollection<ScriptSymbolTag> Tags { get; } = [ScriptSymbolTag.UserDefined];
  public bool IsUserDefined => true;
  public bool IsLambda => false;
  public IReadOnlyList<string> ParameterNames { get; } = Parameters.Select(parameter => parameter.Name).ToArray();
}
