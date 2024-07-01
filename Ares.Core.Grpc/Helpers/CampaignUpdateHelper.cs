using System.Collections.Generic;
using System.Linq;
using Ares.Messaging;
using Microsoft.EntityFrameworkCore;

namespace Ares.Core.Grpc.Helpers;

internal static class CampaignUpdateHelper
{
  public static void UpdateCampaignTemplate(this CampaignTemplate existingTemplate, CampaignTemplate incomingTemplate, DbContext context)
  {
    context.RemoveRange(existingTemplate.ExperimentTemplates.Where(template => incomingTemplate.ExperimentTemplates.All(experimentTemplate => experimentTemplate.UniqueId != template.UniqueId)));
    var existingSteps = existingTemplate.ExperimentTemplates.SelectMany(template => template.StepTemplates).ToList();
    var incomingSteps = incomingTemplate.ExperimentTemplates.SelectMany(template => template.StepTemplates).ToArray();

    existingTemplate.ExperimentTemplates.RemoveExperiments(incomingTemplate.ExperimentTemplates, context);
    existingTemplate.PlannableParameters.RemovePlannedParameters(incomingTemplate.PlannableParameters, context);
    existingSteps.RemoveSteps(incomingSteps, context);
    var existingCommands = existingSteps.SelectMany(template => template.CommandTemplates).ToList();
    var incomingCommands = incomingSteps.SelectMany(template => template.CommandTemplates);

    existingCommands.RemoveCommands(incomingCommands, context);
  }

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

  private static void RemovePlannedParameters(this IList<ParameterMetadata> existingData, IEnumerable<ParameterMetadata> incomingData, DbContext context)
  {
    var removedTemplates = existingData.Where(metadata => incomingData.All(parameterMetadata => parameterMetadata.UniqueId != metadata.UniqueId));
    context.RemoveRange(removedTemplates);
  }

  private static void RemoveExperiments(this IList<ExperimentTemplate> existingData, IEnumerable<ExperimentTemplate> incomingData, DbContext context)
  {
    var removedTemplates = existingData.Where(metadata => incomingData.All(parameterMetadata => parameterMetadata.UniqueId != metadata.UniqueId));
    context.RemoveRange(removedTemplates);
  }

  private static void RemoveSteps(this IList<StepTemplate> existingData, IEnumerable<StepTemplate> incomingData, DbContext context)
  {
    var removedTemplates = existingData.Where(metadata => incomingData.All(parameterMetadata => parameterMetadata.UniqueId != metadata.UniqueId));
    context.RemoveRange(removedTemplates);
  }

  private static void RemoveCommands(this IList<CommandTemplate> existingData, IEnumerable<CommandTemplate> incomingData, DbContext context)
  {
    var removedTemplates = existingData.Where(metadata => incomingData.All(parameterMetadata => parameterMetadata.UniqueId != metadata.UniqueId));
    context.RemoveRange(removedTemplates);
  }
}
