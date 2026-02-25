using Ares.Datamodel;
using Ares.Datamodel.Scripting;

namespace AresScript.Symbols;

public record AresScriptValueSymbol(
  string Name,
  AresValue Value,
  bool IsReadOnly = false,
  SymbolKind SymbolKind = SymbolKind.Variable,
  string? Detail = null,
  string? Documentation = null,
  bool IsUserDefined = true,
  string? ParentName = null) : IValueSymbol
{
}
