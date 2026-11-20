namespace Ares.Core.CustomCommands.Exchange;

public sealed class CustomCommandImportMatch
{
  public required Guid CommandId { get; init; }
  public required string Name { get; init; }
  public required int Score { get; init; }
  public required bool IsExact { get; init; }
  public string Description { get; init; } = string.Empty;
}