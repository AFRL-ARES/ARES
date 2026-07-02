using Ares.Datamodel.Automation;

namespace Ares.Core.CustomCommands;

public interface ICustomCommandPersistenceService
{
  Task<IReadOnlyList<CustomCommandSummary>> GetSummariesAsync();

  Task<IReadOnlyList<CustomCommand>> GetCommandsAsync();

  Task<CustomCommand?> GetAsync(Guid id);

  Task<Guid> SaveAsync(Guid? id, CustomCommand command);

  Task DeleteAsync(Guid id);
}
