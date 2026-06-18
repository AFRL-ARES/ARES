using Ares.Datamodel.Extensions;
using Ares.Datamodel.Planning;
using Ares.Datamodel.Templates;

namespace Ares.Core.Validation.Validators;

public static class AllPlannersAssignedValidator
{
  public static ValidationResult Validate(IEnumerable<Parameter> parameters, IEnumerable<PlannerAllocation> plannerAllocations)
  {
    var plannableParameters = parameters.Where(parameter => parameter.IsPlanned()).ToArray();

    var paramsWithoutPlanningAdapter = plannableParameters
      .Where(parameter => plannerAllocations.All(allocation => allocation.Parameter.UniqueId != parameter.GetPlanningMetadata()?.UniqueId)).ToArray();

    var paramsWithoutPlanner = plannableParameters
      .Where(parameter => parameter.GetPlanningMetadata()?.PlannerName == string.Empty).ToArray();

    if(!paramsWithoutPlanningAdapter.Any() && !paramsWithoutPlanner.Any())
      return new ValidationResult(true);

    var paramsWithoutPlannerNames = paramsWithoutPlanner.Select(parameter => parameter.GetPlanningMetadata()?.Name);
    var paramsWithoutAdapterNames = paramsWithoutPlanningAdapter.Select(parameter => parameter.GetPlanningMetadata()?.Name);

    if(paramsWithoutAdapterNames.Any())
      return new ValidationResult(false, $"Parameters [{string.Join(", ", paramsWithoutAdapterNames)}] do not have a planner adapter properly assigned.");

    return new ValidationResult(false, $"Parameters [{string.Join(", ", paramsWithoutPlannerNames)}] do not have planners properly assigned.");
  }
}
