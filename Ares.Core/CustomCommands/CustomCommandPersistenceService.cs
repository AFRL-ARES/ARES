using Ares.Datamodel.Extensions;
using Microsoft.EntityFrameworkCore;
using CustomCommandModel = Ares.Datamodel.Automation.CustomCommand;

namespace Ares.Core.CustomCommands;

internal sealed class CustomCommandPersistenceService(IDbContextFactory<CoreDatabaseContext> dbContextFactory)
  : ICustomCommandPersistenceService
{
  private const string UniqueIdPropertyName = "UniqueId";

  public async Task<IReadOnlyList<CustomCommandSummary>> GetSummariesAsync()
  {
    await using var context = await dbContextFactory.CreateDbContextAsync();
    var rows = await context.CustomCommands
      .AsNoTracking()
      .Include(command => command.InputParameters)
      .OrderBy(command => command.Name)
      .Select(command => new
      {
        Id = EF.Property<string>(command, UniqueIdPropertyName),
        Command = command
      })
      .ToListAsync();

    return rows
      .Select(row => new CustomCommandSummary(
        ParseEntityId(row.Id),
        string.IsNullOrWhiteSpace(row.Command.Name) ? "(Unnamed command)" : row.Command.Name,
        row.Command.Description,
        BuildInputSummary(row.Command),
        BuildOutputSummary(row.Command)))
      .ToArray();
  }

  public async Task<CustomCommandModel?> GetAsync(Guid id)
  {
    await using var context = await dbContextFactory.CreateDbContextAsync();
    return await context.CustomCommands
      .AsNoTracking()
      .Include(command => command.InputParameters)
      .FirstOrDefaultAsync(command => EF.Property<string>(command, UniqueIdPropertyName) == id.ToString());
  }

  public async Task<Guid> SaveAsync(Guid? id, CustomCommandModel command)
  {
    await using var context = await dbContextFactory.CreateDbContextAsync();
    var commandId = id ?? Guid.NewGuid();
    var existingCommand = id is null
      ? null
      : await context.CustomCommands
        .Include(command => command.InputParameters)
        .FirstOrDefaultAsync(command => EF.Property<string>(command, UniqueIdPropertyName) == id.Value.ToString());

    if(existingCommand is null)
    {
      var commandToAdd = command.Clone();
      context.CustomCommands.Add(commandToAdd);
      AssignEntityId(context, commandToAdd, commandId);
      AssignParameterIds(context, commandToAdd);
    }
    else
    {
      existingCommand.Name = command.Name;
      existingCommand.Description = command.Description;
      existingCommand.OutputSchema = command.OutputSchema?.Clone();
      existingCommand.ScriptBody = command.ScriptBody;

      context.RemoveRange(existingCommand.InputParameters);
      existingCommand.InputParameters.Clear();
      existingCommand.InputParameters.AddRange(command.InputParameters.Select(parameter => parameter.Clone()));
      context.ChangeTracker.DetectChanges();
      AssignParameterIds(context, existingCommand);
    }

    await context.SaveChangesAsync();
    return commandId;
  }

  public async Task DeleteAsync(Guid id)
  {
    await using var context = await dbContextFactory.CreateDbContextAsync();
    var command = await context.CustomCommands
      .FirstOrDefaultAsync(command => EF.Property<string>(command, UniqueIdPropertyName) == id.ToString());

    if(command is null)
    {
      return;
    }

    context.CustomCommands.Remove(command);
    await context.SaveChangesAsync();
  }

  private static Guid ParseEntityId(string? id)
  {
    return Guid.TryParse(id, out var guid) ? guid : Guid.Empty;
  }

  private static void AssignEntityId(CoreDatabaseContext context, CustomCommandModel command, Guid id)
  {
    context.Entry(command).Property<string?>(UniqueIdPropertyName).CurrentValue = id.ToString();
  }

  private static void AssignParameterIds(CoreDatabaseContext context, CustomCommandModel command)
  {
    foreach(var parameter in command.InputParameters)
    {
      var entry = context.Entry(parameter);
      if(entry.State == EntityState.Detached)
      {
        entry.State = EntityState.Added;
      }

      entry.Property<string?>(UniqueIdPropertyName).CurrentValue = Guid.NewGuid().ToString();
    }
  }

  private static string BuildInputSummary(CustomCommandModel command)
  {
    return command.InputParameters.Count == 0
      ? "None"
      : string.Join(", ", command.InputParameters.Select(parameter => parameter.Name));
  }

  private static string BuildOutputSummary(CustomCommandModel command)
  {
    return command.OutputSchema is null
      ? "Unspecified"
      : command.OutputSchema.Stringify();
  }

  public async Task<IReadOnlyList<CustomCommandModel>> GetCommandsAsync()
  {
    await using var context = await dbContextFactory.CreateDbContextAsync();
    return await context.CustomCommands
      .AsNoTracking()
      .Include(command => command.InputParameters)
      .ToListAsync();
  }
}
