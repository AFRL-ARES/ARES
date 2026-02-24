using Ares.Datamodel;
using Ares.Datamodel.Scripting;

namespace AresScript.Symbols;

public delegate Task<AresValue> AresFunctionDelegate(List<AresValue> args, ScriptExecutionControlToken token);

public record AresSystemFunction(
  string Id,
  string Name,
  AresFunctionDelegate Body,
  AresDataSchema InputSchema,
  SchemaEntry OutputSchema,
  string Namespace = "",
  string Description = "",
  bool IsExtension = false,
  string Documentation = "",
  string? ParentName = null) : IFunctionSymbol
{
  public SymbolKind Kind => SymbolKind.Function;
  public string? Detail => Description;
  public bool IsUserDefined => false;
  public bool IsLambda => false;
}
