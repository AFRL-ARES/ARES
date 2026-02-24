using Ares.Datamodel;
using Ares.Datamodel.Scripting;
using AresScript.Generated;

namespace AresScript.Symbols;

public record AresScriptParameter(string Name, AresDataType Type = AresDataType.Any);

public record AresScriptFunction(
  string Name,
  IReadOnlyList<AresScriptParameter> Parameters,
  AresLangParser.FuncBlockContext Body,
  AresDataType ReturnType = AresDataType.Any,
  string? Description = null,
  string? Documentation = null,
  string? ParentName = null) : IFunctionSymbol
{
  public SymbolKind Kind => SymbolKind.Function;
  public string? Detail => Description;
  public bool IsUserDefined => true;
  public bool IsExtension => false;
  public bool IsLambda => false;
  public IReadOnlyList<string> ParameterNames { get; } = Parameters.Select(parameter => parameter.Name).ToArray();
}
