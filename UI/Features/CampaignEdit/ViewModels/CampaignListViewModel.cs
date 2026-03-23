using Ares.Core.Grpc.Services;
using Ares.Datamodel.Templates;
using Ares.Services;
using DynamicData;
using ReactiveUI;
using System.Collections.ObjectModel;

namespace UI.Features.CampaignEdit.ViewModels;

public class CampaignListViewModel : ReactiveObject
{
  private readonly AutomationService _automationClient;
  public readonly ObservableCollection<CampaignTemplateSummary> Templates = [];

  public CampaignListViewModel(AutomationService automationClient)
  {
    _automationClient = automationClient;
  }

  public async Task<CampaignTemplate?> GetFullCampaignTemplate(string campaignId)
  {
    return await _automationClient.GetSingleCampaign(new CampaignRequest { UniqueId = campaignId }, null);
  }

  public async Task<GetCopyOfCampaignResponse> GetCopyOfCampaignTemplate(string campaignId)
  {
    return await _automationClient.GetCopyOfCampaign(new CampaignRequest { UniqueId = campaignId }, null);
  }

  public async Task RefreshCampaigns()
  {
    var campaigns = await _automationClient.GetAllCampaigns(new GetAllCampaignsRequest(), null);
    Templates.Clear();
    Templates.AddRange(campaigns.Campaigns);
  }

  public async Task DeleteCampaign(Guid campaignId)
  {
    await _automationClient.RemoveCampaign(new CampaignRequest { UniqueId = campaignId.ToString() }, null);
    await RefreshCampaigns();
  }
}
