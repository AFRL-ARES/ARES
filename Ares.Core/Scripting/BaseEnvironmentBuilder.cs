using AresScript;
using AresScript.Environment;

namespace Ares.Core.Scripting;

public class BaseEnvironmentBuilder(IEnumerable<ISymbolProvider> symbolProviders)
{
  private readonly IEnumerable<ISymbolProvider> _symbolProviders = symbolProviders;

  public virtual AresScriptEnvironment Build()
  {
    var env = new AresScriptEnvironment();
    var providedSymbols = _symbolProviders.SelectMany(provider => provider.GetSymbols()).ToArray();

    env.AddSystemSymbols(StandardLibrary.Functions);
    env.AddSystemSymbols(providedSymbols);
    env.AssignExtensionFunctions(StandardLibrary.ExtensionFunctions);

    return env;
  }
}
