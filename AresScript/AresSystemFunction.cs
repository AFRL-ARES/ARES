using Ares.Datamodel;

namespace AresScript;

public delegate Task<AresValue> AresFunctionDelegate(List<AresValue> args, ScriptExecutionControlToken token);

public record AresSystemFunction(string Id, string Name, AresFunctionDelegate Body, AresDataSchema InputSchema, SchemaEntry OutputSchema, string Namespace = "", string Description = "") : IFunctionSymbol
{
  public ScriptSymbolKind Kind => ScriptSymbolKind.Function;
  public IReadOnlyCollection<ScriptSymbolTag> Tags { get; } = [ScriptSymbolTag.Extension];
  public bool IsUserDefined => false;
  public bool IsLambda => false;
}
