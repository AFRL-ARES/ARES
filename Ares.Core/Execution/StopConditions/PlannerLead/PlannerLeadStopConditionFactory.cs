using Ares.Core.Planning;

namespace Ares.Core.Execution.StopConditions.PlannerLead;

public class PlannerLeadStopConditionFactory : IPlannerLeadStopConditionFactory
{
  private readonly PlanningResponseRepo _planningResponseRepo;

  public PlannerLeadStopConditionFactory(PlanningResponseRepo planningResponseRepo)
  {
    _planningResponseRepo = planningResponseRepo;
  }

  public PlannerLeadStopCondition Create()
  {
    return new PlannerLeadStopCondition(_planningResponseRepo);
  }
}
