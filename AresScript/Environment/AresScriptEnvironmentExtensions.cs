using AresScript.Symbols;

namespace AresScript.Environment;

public static class AresScriptEnvironmentExtensions
{
  public static void AddSystemSymbol(this AresScriptEnvironment environment, IScriptSymbol symbol)
  {
    EnvironmentSymbolWriter.AddSystemSymbol(environment, symbol);
  }

  public static void AddSystemSymbols(this AresScriptEnvironment environment, IEnumerable<IScriptSymbol> symbols)
  {
    foreach(var symbol in symbols)
    {
      environment.AddSystemSymbol(symbol);
    }
  }

  public static void AddSystemFunction(this AresScriptEnvironment environment, AresSystemFunctionSymbol function)
  {
    EnvironmentSymbolWriter.AddSystemFunction(environment, function);
  }

  public static void AddSystemValue(this AresScriptEnvironment environment, IValueSymbol value)
  {
    EnvironmentSymbolWriter.AddSystemValue(environment, value);
  }
}
