using Ares.Datamodel;

namespace AresScript;

public delegate Task<AresValue> AresFunctionDelegate(IDictionary<string, AresValue> args, ScriptExecutionControlToken token);

public record AresSystemFunction(string Id, string Name, AresFunctionDelegate Body, AresStructSchema InputSchema, AresValueSchema OutputSchema, string Namespace = "", string Description = "")
{
}