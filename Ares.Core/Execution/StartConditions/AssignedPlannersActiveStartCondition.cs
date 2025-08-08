using Ares.Core.Planning;
using Ares.Datamodel.Planning;

namespace Ares.Core.Execution.StartConditions;

public class AssignedPlannersActiveStartCondition : IStartCondition
{
  private readonly IActiveCampaignTemplateStore _activeCampaignTemplateStore;
  private readonly IPlannerManager _plannerManager;

  public AssignedPlannersActiveStartCondition(IActiveCampaignTemplateStore activeCampaignTemplateStore, IPlannerManager plannerManager)
  {
    _activeCampaignTemplateStore = activeCampaignTemplateStore;
    _plannerManager = plannerManager;
  }

  public Task<StartConditionResult> CanStart()
  {
    if(_activeCampaignTemplateStore.CampaignTemplate?.ExperimentTemplates.All(template => template is null) ?? true)
      return null;

    var assignedPlanners = _activeCampaignTemplateStore.CampaignTemplate.PlannerAllocations.Select(allocation => allocation.Planner);

    foreach(var adapter in assignedPlanners)
    {
      var planner = _plannerManager.GetPlannerByName(adapter.AdapterName);

      if(planner is null)
        return Task.FromResult(new StartConditionResult(false, $"Assigned Planner Adapter {adapter.AdapterName} could not be found in the core."));

      if(planner.Status.PlannerState != PlannerState.Active)
        return Task.FromResult(new StartConditionResult(false, $"Assigned Planner Adapter {adapter.AdapterName} is not connected to ARES."));
    }

    return Task.FromResult(new StartConditionResult(true));
  }

}
