using Ares.Datamodel.Automation;

namespace Ares.Core.CustomCommands;

public interface ICustomCommandPersistenceService
{
  Task<IReadOnlyList<CustomCommandSummary>> GetSummariesAsync();

  Task<IReadOnlyList<CustomCommandVersion>> GetCommandsAsync();

  Task<CustomCommandVersion?> GetAsync(Guid id);

  Task<Guid> SaveAsync(Guid? id, CustomCommandVersion command);

  Task DeleteAsync(Guid id);
}
