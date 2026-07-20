namespace Ares.Core.CustomCommands;

public sealed record CustomCommandSummary(
  Guid Id,
  string Name,
  string Description,
  string InputSummary,
  string OutputSummary);
