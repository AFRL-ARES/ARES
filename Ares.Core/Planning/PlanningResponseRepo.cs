using Ares.Datamodel.Planning;
using System.Collections.ObjectModel;

namespace Ares.Core.Planning;

public class PlanningResponseRepo : Collection<PlanningResponse>
{
  public void StorePlanResponse(PlanningResponse planResponse)
  {
    Add(planResponse);
  }

  public void ClearPlanResponses()
  {
    Clear();
  }
}
