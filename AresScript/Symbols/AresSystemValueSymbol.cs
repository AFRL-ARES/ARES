using Ares.Datamodel;
using Ares.Datamodel.Scripting;

namespace AresScript.Symbols;

public record AresSystemValueSymbol(
  string Name,
  AresSystemValue SystemValue,
  bool IsReadOnly = true,
  SymbolKind Kind = SymbolKind.Variable,
  string? ParentName = null) : IValueSymbol
{
  public string? Detail => SystemValue.Description;
  public string? Documentation => SystemValue.Description;
  public bool IsUserDefined => false;
  public AresValue Value => AresSystemValueExtensions.ToAresValue(SystemValue);
}
