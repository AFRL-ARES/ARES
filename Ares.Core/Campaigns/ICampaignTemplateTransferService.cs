using Ares.Core.Campaigns.Models;

namespace Ares.Core.Campaigns;

public interface ICampaignTemplateTransferService
{
  Task<CampaignTemplateExport?> ExportAsync(string campaignId, CancellationToken cancellationToken = default);

  Task<CampaignImportAnalysis> AnalyzeImportAsync(string json, CancellationToken cancellationToken = default);

  Task<CampaignTemplateImportResult> ImportAsync(string json, CancellationToken cancellationToken = default);

  Task<CampaignTemplateImportResult> CommitImportAsync(CampaignImportAnalysis analysis, CancellationToken cancellationToken = default);
}