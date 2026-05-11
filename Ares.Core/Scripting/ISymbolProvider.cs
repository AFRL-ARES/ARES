using AresScript.Symbols;

namespace Ares.Core.Scripting;

public interface ISymbolProvider
{
  Task<IScriptSymbol[]> GetSymbols();
}
