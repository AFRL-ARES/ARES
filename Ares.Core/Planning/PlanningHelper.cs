using Ares.Messaging;

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
    CancellationToken cancellationToken)
  {
    var parameterArray = parameters.ToArray();
    var plannerToMetadataMaps = new List<(IPlanner Planner, ParameterMetadata Metadata)>();
    foreach (var plannerAllocation in plannerAllocations)
    {
      var hasVersion = Version.TryParse(plannerAllocation.Planner.Version, out var version);
      var planner = hasVersion
        ? _plannerManager.GetPlanner(plannerAllocation.Planner.Type, plannerAllocation.Planner.Name, version!)
        : _plannerManager.GetPlanner(plannerAllocation.Planner.Type, plannerAllocation.Planner.Name);

      plannerToMetadataMaps.Add((planner, plannerAllocation.Parameter));
    }

    var planGroup = plannerToMetadataMaps.GroupBy(pair => pair.Planner);
    var seedAnalysesArr = seedAnalyses.ToArray();
    foreach (var grouping in planGroup)
    {
      var planner = grouping.Key;
      var resultsEnumerable = await planner.Plan(grouping.Select(pair => pair.Metadata), seedAnalysesArr, cancellationToken);
      var results = resultsEnumerable.ToArray();
      if (!results.Any())
        return false;

      foreach (var result in results)
      {
        var parameterPlanTarget = parameterArray.First(parameter => parameter.PlanningMetadata.UniqueId == result.Metadata.UniqueId);
        var val = new ParameterValue
        {
          UniqueId = Guid.NewGuid().ToString(),
          Value = Convert.ToSingle(result.Value)
        };

        parameterPlanTarget.Value = val;
      }
    }

    return true;
  }
}
