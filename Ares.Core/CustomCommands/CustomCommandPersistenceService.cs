using Ares.Datamodel.Automation;
using Ares.Datamodel.Extensions;
using Microsoft.EntityFrameworkCore;
using CustomCommandModel = Ares.Datamodel.Automation.CustomCommand;
using CustomCommandVersionModel = Ares.Datamodel.Automation.CustomCommandVersion;

namespace Ares.Core.CustomCommands;

internal sealed class CustomCommandPersistenceService(IDbContextFactory<CoreDatabaseContext> dbContextFactory)
  : ICustomCommandPersistenceService
{
  private const string UniqueIdPropertyName = "UniqueId";

  public async Task<IReadOnlyList<CustomCommandSummary>> GetSummariesAsync()
  {
    var commands = await GetCurrentVersionsAsync();
    return commands
      .Select(command => new CustomCommandSummary(
        ParseEntityId(command.CustomCommandId),
        string.IsNullOrWhiteSpace(command.Name) ? "(Unnamed command)" : command.Name,
        command.Description,
        BuildInputSummary(command),
        BuildOutputSummary(command)))
      .OrderBy(command => command.Name)
      .ToArray();
  }

  public async Task<CustomCommandVersionModel?> GetAsync(Guid id)
  {
    await using var context = await dbContextFactory.CreateDbContextAsync();
    var currentVersionId = await context.CustomCommands
      .AsNoTracking()
      .Where(command => EF.Property<string>(command, UniqueIdPropertyName) == id.ToString())
      .Select(command => command.CurrentVersionId)
      .FirstOrDefaultAsync();

    return string.IsNullOrWhiteSpace(currentVersionId)
      ? null
      : await GetVersionAsync(context, currentVersionId);
  }

  public async Task<Guid> SaveAsync(Guid? id, CustomCommandVersionModel command)
  {
    await using var context = await dbContextFactory.CreateDbContextAsync();
    var commandId = id ?? Guid.NewGuid();
    var existingCommand = id is null
      ? null
      : await context.CustomCommands
        .FirstOrDefaultAsync(command => EF.Property<string>(command, UniqueIdPropertyName) == id.Value.ToString());

    var nextVersionNumber = existingCommand is null
      ? 1
      : await context.CustomCommandVersions
        .Where(version => version.CustomCommandId == commandId.ToString())
        .Select(version => (long?)version.VersionNumber)
        .MaxAsync() ?? 0;

    if(existingCommand is not null)
      nextVersionNumber++;

    var version = CreateVersion(commandId, nextVersionNumber, command);
    context.CustomCommandVersions.Add(version);
    var versionId = AssignEntityId(context, version);
    AssignParameterIds(context, version);

    if(existingCommand is null)
    {
      existingCommand = new CustomCommandModel();
      context.CustomCommands.Add(existingCommand);
      AssignEntityId(context, existingCommand, commandId);
    }

    existingCommand.CurrentVersionId = versionId.ToString();
    await context.SaveChangesAsync();
    return commandId;
  }

  public async Task DeleteAsync(Guid id)
  {
    await using var context = await dbContextFactory.CreateDbContextAsync();
    var command = await context.CustomCommands
      .FirstOrDefaultAsync(command => EF.Property<string>(command, UniqueIdPropertyName) == id.ToString());

    if(command is null)
      return;

    context.CustomCommands.Remove(command);
    await context.SaveChangesAsync();
  }

  public Task<IReadOnlyList<CustomCommandVersionModel>> GetCommandsAsync() => GetCurrentVersionsAsync();

  private async Task<IReadOnlyList<CustomCommandVersionModel>> GetCurrentVersionsAsync()
  {
    await using var context = await dbContextFactory.CreateDbContextAsync();
    var versionIds = await context.CustomCommands
      .AsNoTracking()
      .Select(command => command.CurrentVersionId)
      .Where(id => id != null && id != string.Empty)
      .ToListAsync();

    return await context.CustomCommandVersions
      .AsNoTracking()
      .Include(version => version.InputParameters)
      .Where(version => versionIds.Contains(EF.Property<string>(version, UniqueIdPropertyName)))
      .ToListAsync();
  }

  private static Task<CustomCommandVersionModel?> GetVersionAsync(CoreDatabaseContext context, string versionId)
  {
    return context.CustomCommandVersions
      .AsNoTracking()
      .Include(version => version.InputParameters)
      .FirstOrDefaultAsync(version => EF.Property<string>(version, UniqueIdPropertyName) == versionId);
  }

  private static Guid ParseEntityId(string? id) => Guid.TryParse(id, out var guid) ? guid : Guid.Empty;

  private static void AssignEntityId(CoreDatabaseContext context, CustomCommandModel command, Guid id)
  {
    command.UniqueId = id.ToString();
    context.Entry(command).Property<string?>(UniqueIdPropertyName).CurrentValue = command.UniqueId;
  }

  private static Guid AssignEntityId(CoreDatabaseContext context, CustomCommandVersionModel version)
  {
    var id = Guid.NewGuid();
    version.UniqueId = id.ToString();
    context.Entry(version).Property<string?>(UniqueIdPropertyName).CurrentValue = version.UniqueId;
    return id;
  }

  private static void AssignParameterIds(CoreDatabaseContext context, CustomCommandVersionModel version)
  {
    foreach(var parameter in version.InputParameters)
    {
      var entry = context.Entry(parameter);
      if(entry.State == EntityState.Detached)
        entry.State = EntityState.Added;

      entry.Property<string?>(UniqueIdPropertyName).CurrentValue = Guid.NewGuid().ToString();
    }
  }

  private static CustomCommandVersionModel CreateVersion(
    Guid commandId,
    long versionNumber,
    CustomCommandVersionModel command)
  {
    var version = command.Clone();
    version.UniqueId = string.Empty;
    version.CustomCommandId = commandId.ToString();
    version.VersionNumber = versionNumber;
    return version;
  }

  private static string BuildInputSummary(CustomCommandVersionModel command)
  {
    return command.InputParameters.Count == 0
      ? "None"
      : string.Join(", ", command.InputParameters.Select(parameter => parameter.Name));
  }

  private static string BuildOutputSummary(CustomCommandVersionModel command)
  {
    return command.OutputSchema is null
      ? "Unspecified"
      : command.OutputSchema.Stringify();
  }
}
