using Ares.Datamodel.Templates;

namespace Ares.Core.Execution;

internal class ActiveCampaignTemplateStore : IActiveCampaignTemplateStore
{
  public CampaignTemplate? CampaignTemplate { get; set; }
}
