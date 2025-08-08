using Ares.Datamodel.Templates;
using Ares.Messaging.Planning;
using Google.Protobuf.WellKnownTypes;
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
    var planners = await _client.GetAllPlannersAsync(new Empty());
    return new PlanningViewModel(template, planners.Planners, _client);
  }
}
