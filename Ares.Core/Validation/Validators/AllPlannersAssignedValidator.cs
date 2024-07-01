using Ares.Messaging;

namespace Ares.Core.Validation.Validators;

public static class AllPlannersAssignedValidator
{
  public static ValidationResult Validate(IEnumerable<Parameter> parameters, IEnumerable<PlannerAllocation> plannerAllocations)
  {
    var plannableParameters = parameters.Where(parameter => parameter.Planned).ToArray();

    var paramsWithoutPlanner = plannableParameters.Where(parameter => plannerAllocations.All(allocation => allocation.Parameter.UniqueId != parameter.PlanningMetadata.UniqueId)).ToArray();
    if (!paramsWithoutPlanner.Any())
      return new ValidationResult(true);

    var paramsWithoutPlannerNames = paramsWithoutPlanner.Select(parameter => parameter.PlanningMetadata.Name);
    return new ValidationResult(false, $"Parameters [{string.Join(", ", paramsWithoutPlannerNames)}] do not have planners assigned.");
  }
}
