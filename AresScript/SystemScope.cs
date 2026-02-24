using AresScript.Symbols;

namespace AresScript;

public class SystemScope(string name = "")
{
  public string Name { get; } = name;

  public Dictionary<string, AresSystemValueSymbol> Variables { get; } = [];

  public Dictionary<string, AresSystemFunction> Functions { get; } = [];

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
  }
}
