using Ares.Core.Planning;
using Ares.Datamodel.Planning;

namespace Ares.Core.Execution.StopConditions.PlannerLead;

public class PlannerLeadStopCondition : IStopCondition
{
  private readonly PlanningResponseRepo _responseRepo;

  public PlannerLeadStopCondition(PlanningResponseRepo responseRepo)
  {
    _responseRepo = responseRepo;
  }

  public string Message { get; private set; } = "";

  public string Description => $"Will stop when planner reports objective is achieved";

  public bool ShouldStop()
  {
    var latestPlanResponse = _responseRepo.LastOrDefault();
    if(latestPlanResponse is null)
      return false;

    var objectiveStatus = latestPlanResponse.ObjectiveStatus;
    var resultAchieved = objectiveStatus == ObjectiveStatus.ObjectiveAchieved;

    if(resultAchieved)
      Message = $"Planner has reported our objective was achieved, stopping the campaign.";

    return resultAchieved;
  }
}
