using Ares.Datamodel;
using Ares.Datamodel.Planning;

namespace Ares.Core.Planning;

public interface IPlannerServiceCache
{
  Task CachePlannerInfo(RemotePlannerService planner);
  Task CachePlannerSettings(RemotePlannerService planner);
  Task<PlannerServiceInfo?> GetCachedPlannerInfo(string plannerId);
  Task<AresStruct?> GetCachedPlannerSettings(string plannerId);
}
