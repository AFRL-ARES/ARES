using AresScript.Symbols;

namespace Ares.Core.Scripting;

public interface ISystemFunctionProvider
{
  AresSystemFunction[] GetFunctions();
}
