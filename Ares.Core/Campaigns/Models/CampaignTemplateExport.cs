using Ares.Datamodel.Templates;

namespace Ares.Core.Campaigns.Models;

public sealed record CampaignTemplateExport(CampaignTemplate Template, string Json, string SuggestedFileName);