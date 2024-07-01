using Ares.Messaging;

namespace Ares.Core.Execution;

internal class ActiveCampaignTemplateStore : IActiveCampaignTemplateStore
{
  public CampaignTemplate? CampaignTemplate { get; set; }
}
