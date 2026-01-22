using Ares.Datamodel;

namespace AresScript;

public delegate Task<AresValue> AresFunctionDelegate(List<AresValue> args, CancellationToken token);

public record AresSystemFunction(string Id, string Name, AresFunctionDelegate Body, AresDataSchema InputSchema, AresDataSchema OutputSchema, string Namespace = "", string Description = "")
{
}
