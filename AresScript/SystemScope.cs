using Ares.Datamodel;

namespace AresScript;

public class SystemScope(string name = "")
{
  public string Name { get; } = name;

  public Dictionary<string, AresValue> Variables { get; } = [];

  public Dictionary<string, AresSystemFunction> SystemFunctions { get; } = [];
}
