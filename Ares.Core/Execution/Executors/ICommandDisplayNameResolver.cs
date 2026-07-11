using Ares.Datamodel.Templates;

namespace Ares.Core.Execution.Executors;

public interface ICommandDisplayNameResolver
{
  Task RefreshAsync();

  string Resolve(CommandTemplate template);
}
