using AresScript.Symbols;

namespace AresScript.Environment;

public class UserScope(string name = "")
{
  public string Name { get; } = name;

  public Dictionary<string, AresSystemValue> Variables { get; } = [];

  public Dictionary<string, AresScriptFunction> Functions { get; } = [];

  public Dictionary<string, AresScriptLambda> Lambdas { get; } = [];

  public IEnumerable<IScriptSymbol> GetSymbols()
  {
    foreach(var symbol in Variables.Values)
    {
      yield return symbol;
    }

    foreach(var symbol in Functions.Values)
    {
      yield return symbol;
    }

    foreach(var symbol in Lambdas.Values)
    {
      yield return symbol;
    }
  }
}
