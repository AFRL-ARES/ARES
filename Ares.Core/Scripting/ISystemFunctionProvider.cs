using AresScript;

namespace Ares.Core.Scripting;

public interface ISystemFunctionProvider
{
  AresSystemFunction[] GetFunctions();
}
