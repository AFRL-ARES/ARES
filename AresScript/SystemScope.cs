using Ares.Datamodel;

namespace AresScript;

public class SystemScope(string name = "")
{
  public string Name { get; } = name;

  public Dictionary<string, AresSystemValue> Variables { get; } = [];

  public Dictionary<string, AresSystemFunction> Functions { get; } = [];
}
