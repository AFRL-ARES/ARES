using Ares.Core.CustomCommands;
using Ares.Datamodel.Templates;
using Microsoft.Extensions.Logging;

namespace Ares.Core.Execution.Executors;

internal class CommandDisplayNameResolver(
  ICustomCommandPersistenceService customCommandPersistenceService,
  ILogger<CommandDisplayNameResolver> logger) : ICommandDisplayNameResolver
{
  private IReadOnlyDictionary<string, string> _customCommandNames = CreateEmptyLookup();

  public async Task RefreshAsync()
  {
    try
    {
      var commands = await customCommandPersistenceService.GetCommandsAsync();
      _customCommandNames = commands
        .Where(command => !string.IsNullOrWhiteSpace(command.CustomCommandId) && !string.IsNullOrWhiteSpace(command.Name))
        .GroupBy(command => command.CustomCommandId, StringComparer.OrdinalIgnoreCase)
        .ToDictionary(
          group => group.Key,
          group => group.First().Name.Trim(),
          StringComparer.OrdinalIgnoreCase);
    }
    catch(Exception ex)
    {
      _customCommandNames = CreateEmptyLookup();
      logger.LogWarning(ex, "Could not load custom-command names. Generic names will be used for this campaign.");
    }
  }

  public string Resolve(CommandTemplate template)
    => template.CommandTypeCase switch
    {
      CommandTemplate.CommandTypeOneofCase.None => "Undefined Command",
      CommandTemplate.CommandTypeOneofCase.DeviceCommand => template.DeviceCommand.Metadata.Name,
      CommandTemplate.CommandTypeOneofCase.SystemCommand => template.SystemCommand.Operation.ToString(),
      CommandTemplate.CommandTypeOneofCase.CustomCommandInvocation => ResolveCustomCommand(template.CustomCommandInvocation.CustomCommandId),
      _ => "Undefined Command"
    };

  private string ResolveCustomCommand(string customCommandId)
    => !string.IsNullOrWhiteSpace(customCommandId) && _customCommandNames.TryGetValue(customCommandId, out var name)
      ? name
      : "Custom Command";

  private static IReadOnlyDictionary<string, string> CreateEmptyLookup()
    => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
