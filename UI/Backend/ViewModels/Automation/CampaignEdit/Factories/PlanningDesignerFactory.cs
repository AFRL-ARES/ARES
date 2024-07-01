using Ares.Messaging;
using UI.Backend.ViewModels.Automation.CampaignEdit;

namespace UI.Backend.ViewModels.Factories;

public class PlanningDesignerFactory
{
  private readonly AresPlanning.AresPlanningClient _client;

  public PlanningDesignerFactory(AresPlanning.AresPlanningClient client)
  {
    _client = client;
  }
  
  public async Task<PlanningViewModel> Create(CampaignTemplate template)
  {
    var planners = await _client.GetAllPlannersAsync(new GetAllPlannersRequest());
    return new PlanningViewModel(template, planners.Planners);
  }
}
