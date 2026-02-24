using AresScript.Symbols;

namespace Ares.Core.Scripting;

public interface ISymbolProvider
{
  IScriptSymbol[] GetSymbols();
}
