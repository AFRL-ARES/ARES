using Ares.Core.Campaigns;
using Ares.Core.Grpc.Services;
using Ares.Datamodel.Templates;
using Ares.Services;
using DynamicData;
using ReactiveUI;
using System.Collections.ObjectModel;
using Microsoft.AspNetCore.Components.Forms;

namespace UI.Features.CampaignEdit.ViewModels;

public class CampaignListViewModel : ReactiveObject
{
  private readonly AutomationService _automationClient;
  private readonly ICampaignTemplateTransferService _campaignTemplateTransferService;
  public const long MaximumImportFileSize = 5 * 1024 * 1024;
  public readonly ObservableCollection<CampaignTemplateSummary> Templates = [];

  public CampaignListViewModel(
    AutomationService automationClient,
    ICampaignTemplateTransferService campaignTemplateTransferService)
  {
    _automationClient = automationClient;
    _campaignTemplateTransferService = campaignTemplateTransferService;
  }

  public async Task<CampaignTemplate?> GetFullCampaignTemplate(string campaignId)
  {
    return await _automationClient.GetSingleCampaign(new CampaignRequest { UniqueId = campaignId }, null);
  }

  public Task<CampaignTemplateExport?> ExportCampaignTemplate(string campaignId)
    => _campaignTemplateTransferService.ExportAsync(campaignId);

  public async Task<CampaignTemplateImportResult> ImportCampaignTemplate(IBrowserFile file)
  {
    if(file.Size > MaximumImportFileSize)
      throw new CampaignTemplateImportException("Campaign template files must be 5 MiB or smaller.");

    await using var stream = file.OpenReadStream(MaximumImportFileSize);
    using var reader = new StreamReader(stream);
    return await _campaignTemplateTransferService.ImportAsync(await reader.ReadToEndAsync());
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
