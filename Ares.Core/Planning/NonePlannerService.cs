using Ares.Datamodel;
using Ares.Datamodel.Analyzing;
using Ares.Datamodel.Planning;
using Ares.Datamodel.Templates;

namespace Ares.Core.Planning;

public class NonePlannerService : PlannerServiceBase
{
  public NonePlannerService() : base("NONE", "NONE", "1.0.0")
  {
    UniqueId = "NONE-PLANNER";
  }

  public override Task<PlannerServiceCapabilities> GetCapabilities(CancellationToken cancellationToken)
  {
    var capability = new PlannerServiceCapabilities { TimeoutSeconds = long.MaxValue };
    return Task.FromResult(capability);
  }

  public override Task<IEnumerable<PlanResult>> Plan(IEnumerable<ParameterMetadata> plannableParameters, IEnumerable<ExperimentOverview> previousExperiments, IEnumerable<Analysis> analysisHistory, CancellationToken cancellationToken = default)
  { 
    return Task.FromResult(Enumerable.Empty<PlanResult>());
  }

  public override Task<IEnumerable<PlanResult>> Plan(IEnumerable<ParameterMetadata> plannableParameters, IEnumerable<ExperimentOverview> previousExperiments, IEnumerable<Analysis> analysisHistory, AresStruct settings, CancellationToken cancellationToken = default)
  {
    return Plan(plannableParameters, previousExperiments, analysisHistory, cancellationToken);
  }
}
