using Ares.Datamodel;
using Ares.Datamodel.Scripting;

namespace AresScript.Symbols;

public delegate Task<AresValue> AresFunctionDelegate(List<AresValue> args, ScriptExecutionControlToken token);

public record StaticArgValidation(bool Success, string? Error = null, int Index = 0);

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

  /// <summary>
  /// Optional best-effort static-analysis validator called during script validation after
  /// schema validation. Return an error message to fail validation, or null to pass.
  /// Arguments may be exact constant values, environment-resolved values, or typed dummy
  /// values produced by inference when the original expression cannot be fully resolved.
  /// </summary>
  public Func<IReadOnlyList<AresValue?>, StaticArgValidation>? StaticArgumentValidator { get; set; }
}
