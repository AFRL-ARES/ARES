using Ares.Messaging;

namespace Ares.Core.Validation.Validators;

public static class AllPlannersAssignedValidator
{
  public static ValidationResult Validate(IEnumerable<Parameter> parameters, IEnumerable<PlannerAllocation> plannerAllocations)
  {
    var plannableParameters = parameters.Where(parameter => parameter.Planned).ToArray();

    var paramsWithoutPlanningAdapter = plannableParameters
      .Where(parameter => plannerAllocations.All(allocation => allocation.Parameter.UniqueId != parameter.PlanningMetadata.UniqueId)).ToArray();

    var paramsWithoutPlanner = plannableParameters
      .Where(parameter => parameter.PlanningMetadata.PlannerName == string.Empty).ToArray();

    if(!paramsWithoutPlanningAdapter.Any() && !paramsWithoutPlanner.Any())
      return new ValidationResult(true);

    var paramsWithoutPlannerNames = paramsWithoutPlanner.Select(parameter => parameter.PlanningMetadata.Name);
    var paramsWithoutAdapterNames = paramsWithoutPlanningAdapter.Select(parameter => parameter.PlanningMetadata.Name);

    if(paramsWithoutAdapterNames.Any())
      return new ValidationResult(false, $"Parameters [{string.Join(", ", paramsWithoutAdapterNames)}] do not have a planner adapter properly assigned.");

    return new ValidationResult(false, $"Parameters [{string.Join(", ", paramsWithoutPlannerNames)}] do not have planners properly assigned.");
  }
}
