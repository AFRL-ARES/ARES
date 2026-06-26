using CustomCommandModel = Ares.Datamodel.Automation.CustomCommand;

namespace Ares.Core.CustomCommands;

public interface ICustomCommandPersistenceService
{
  Task<IReadOnlyList<CustomCommandSummary>> GetSummariesAsync();

  Task<CustomCommandModel?> GetAsync(Guid id);

  Task<Guid> SaveAsync(Guid? id, CustomCommandModel command);

  Task DeleteAsync(Guid id);
}
