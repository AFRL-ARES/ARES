namespace Ares.Core.CustomCommands.Exchange;

public sealed record CustomCommandImportResult(Guid CommandId, string CommandName, IReadOnlyList<string> Warnings);