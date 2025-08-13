using Ares.Datamodel;
using Ares.Datamodel.Analyzing;
using Ares.Datamodel.Templates;

namespace Ares.Core.Planning;

public class PlanningHelper : IPlanningHelper
{
  private readonly IPlannerManager _plannerManager;

  public PlanningHelper(IPlannerManager plannerManager)
  {
    _plannerManager = plannerManager;
  }

  public async Task<bool> TryResolveParameters(IEnumerable<PlannerAllocation> plannerAllocations,
    IEnumerable<Parameter> parameters,
    IEnumerable<Analysis> seedAnalyses,
    IEnumerable<ExperimentOverview> seedExperiments,
    CancellationToken cancellationToken)
  {
    var parameterArray = parameters.ToArray();
    var plannerToMetadataMaps = new List<(IPlanner Planner, ParameterMetadata Metadata)>();
    foreach(var plannerAllocation in plannerAllocations)
    {
      var hasVersion = Version.TryParse(plannerAllocation.Planner.Version, out var version);
      var planner = hasVersion
        ? _plannerManager.GetPlanner(plannerAllocation.Planner.Type, plannerAllocation.Planner.AdapterName, version!)
        : _plannerManager.GetPlanner(plannerAllocation.Planner.Type, plannerAllocation.Planner.AdapterName);

      plannerToMetadataMaps.Add((planner, plannerAllocation.Parameter));
    }

    var planGroup = plannerToMetadataMaps.GroupBy(pair => pair.Planner);
    var seedAnalysesArr = seedAnalyses.ToArray();
    foreach(var grouping in planGroup)
    {
      var planner = grouping.Key;
      var resultsEnumerable = await planner.Plan(grouping.Select(pair => pair.Metadata), seedExperiments, seedAnalysesArr, cancellationToken);
      var results = resultsEnumerable.ToArray();
      if(!results.Any())
        return false;

      foreach(var result in results)
      {
        var parameterPlanTarget = parameterArray.FirstOrDefault(parameter => parameter.PlanningMetadata.UniqueId == result.Metadata.UniqueId);

        if(parameterPlanTarget is null)
          continue;

        parameterPlanTarget.Value = result.Value;
      }
    }

    return true;
  }
}
