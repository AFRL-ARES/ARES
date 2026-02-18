using Ares.Datamodel;

namespace AresScript;

public class UserScope(string name = "")
{
  public string Name { get; } = name;

  public Dictionary<string, AresValue> Variables { get; } = [];

  public Dictionary<string, AresScriptFunction> Functions { get; } = [];

  public Dictionary<string, AresScriptLambda> Lambdas { get; } = [];
}
