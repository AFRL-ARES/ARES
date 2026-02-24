using Ares.Datamodel;
using Ares.Datamodel.Scripting;
using AresScript.Generated;

namespace AresScript.Symbols;

public record AresScriptLambda(
  string Name,
  IReadOnlyList<string> Parameters,
  AresLangParser.ExpressionContext Body,
  IReadOnlyDictionary<string, AresValue> Closure,
  string? Description = null,
  string? Documentation = null,
  string? ParentName = null) : IFunctionSymbol
{
  public SymbolKind Kind => SymbolKind.Function;
  public string? Detail => Description;
  public bool IsUserDefined => true;
  public bool IsExtension => false;
  public bool IsLambda => true;
}
