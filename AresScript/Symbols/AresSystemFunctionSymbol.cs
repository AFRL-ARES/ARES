using Ares.Datamodel;
using Ares.Datamodel.Scripting;

namespace AresScript.Symbols;

public delegate Task<AresValue> AresFunctionDelegate(List<AresValue> args, ScriptExecutionControlToken token);

public record AresSystemFunctionSymbol(
  string Id,
  string Name,
  AresFunctionDelegate Body,
  AresDataSchema InputSchema,
  SchemaEntry OutputSchema,
  string Namespace = "",
  bool IsExtension = false,
  string? ParentName = null) : IFunctionSymbol
{
  public SymbolKind SymbolKind => SymbolKind.Function;
  public string? Detail { get; set; }
  public string? Documentation { get; set; }
  public bool IsUserDefined => false;
  public bool IsLambda => false;
}
