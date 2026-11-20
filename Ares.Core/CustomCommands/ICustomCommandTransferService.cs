using Ares.Core.CustomCommands.Exchange;

namespace Ares.Core.CustomCommands;

public interface ICustomCommandTransferService
{
  Task<CustomCommandExport?> ExportAsync(Guid commandId, CancellationToken cancellationToken = default);

  Task<CustomCommandImportAnalysis> AnalyzeImportAsync(string json, CancellationToken cancellationToken = default);

  Task<CustomCommandImportResult> ImportAsync(CustomCommandImportAnalysis analysis, CancellationToken cancellationToken = default);
}