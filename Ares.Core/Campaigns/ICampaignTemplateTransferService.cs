using Ares.Datamodel.Templates;

namespace Ares.Core.Campaigns;

public interface ICampaignTemplateTransferService
{
  Task<CampaignTemplateExport?> ExportAsync(string campaignId, CancellationToken cancellationToken = default);

  Task<CampaignTemplateImportResult> ImportAsync(string json, CancellationToken cancellationToken = default);
}

public sealed record CampaignTemplateExport(CampaignTemplate Template, string Json, string SuggestedFileName);

public sealed record CampaignTemplateImportResult(CampaignTemplate Template, IReadOnlyList<string> Warnings);
