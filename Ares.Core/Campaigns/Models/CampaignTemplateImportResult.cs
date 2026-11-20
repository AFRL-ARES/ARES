using Ares.Datamodel.Templates;

namespace Ares.Core.Campaigns.Models;

public sealed record CampaignTemplateImportResult(CampaignTemplate Template, IReadOnlyList<string> Warnings);