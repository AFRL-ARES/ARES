using Ares.Messaging;
using DynamicData;
using Google.Protobuf.WellKnownTypes;
using ReactiveUI;
using System.Collections.ObjectModel;
using UI.Settings;

namespace UI.Backend.ViewModels.Automation.CampaignEdit;

public class CampaignListViewModel : ReactiveObject
{
  private readonly AresAutomation.AresAutomationClient _automationClient;

  public readonly ObservableCollection<CampaignTemplate> Templates = new();

  public CampaignListViewModel(AresAutomation.AresAutomationClient automationClient, IConfiguration configuration)
  {
    _automationClient = automationClient;
  }

  public async Task RefreshCampaigns()
  {
    var campaigns = await _automationClient.GetAllCampaignsAsync(new GetAllCampaignsRequest());
    Templates.Clear();
    Templates.AddRange(campaigns.CampaignTemplates);
  }

  public async Task DeleteCampaign(Guid campaignId)
  {
    await _automationClient.RemoveCampaignAsync(new CampaignRequest { UniqueId = campaignId.ToString() });
    await RefreshCampaigns();
  }
}
