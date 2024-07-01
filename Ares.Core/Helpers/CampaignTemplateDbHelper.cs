using Ares.Messaging;

namespace Ares.Core.Helpers;

internal static class CampaignTemplateDbHelper
{
  public static void ConsolidatePlannedParameterMetadata(this CampaignTemplate template)
  {
    var commandParams = template.ExperimentTemplates
      .SelectMany(experimentTemplate => experimentTemplate.StepTemplates)
      .SelectMany(stepTemplate => stepTemplate.CommandTemplates)
      .SelectMany(commandTemplate => commandTemplate.Parameters)
      .Where(param => param.PlanningMetadata is not null);

    foreach (var commandParam in commandParams)
      commandParam.PlanningMetadata = template.PlannableParameters.First(metadata => metadata.UniqueId == commandParam.PlanningMetadata.UniqueId);

    foreach (var allocation in template.PlannerAllocations)
    {
      // TODO maybe a better way to do this
      allocation.Parameter = template.PlannableParameters.First(metadata => metadata.UniqueId == allocation.Parameter.UniqueId);
      allocation.Planner = template.PlannerAllocations.First(plannerAllocation => plannerAllocation.Planner.UniqueId == allocation.Planner.UniqueId).Planner;
    }
  }
}
