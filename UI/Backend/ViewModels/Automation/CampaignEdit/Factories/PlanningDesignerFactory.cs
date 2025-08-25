using Ares.Datamodel.Templates;
using Ares.Services;
using Google.Protobuf.WellKnownTypes;
using UI.Backend.ViewModels.Automation.CampaignEdit;


namespace UI.Backend.ViewModels.Factories;

public class PlanningDesignerFactory
{
  private readonly AresPlannerManagementService.AresPlannerManagementServiceClient _client;

  public PlanningDesignerFactory(AresPlannerManagementService.AresPlannerManagementServiceClient client)
  {
    _client = client;
  }

  public async Task<PlanningViewModel> Create(CampaignTemplate template)
  {
    var planners = await _client.GetAllPlannersAsync(new Empty());
    return new PlanningViewModel(template, planners.Planners, _client);
  }
}
