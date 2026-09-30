using Ares.Datamodel.Planning;

namespace Ares.Core.Planning;

public interface IRemotePlannerManager
{
  Task LoadPlanners();

  Task CreatePlanner(string name, string url);

  Task RemovePlanner(string plannerId);

  Task UpdatePlanner(PlannerConfig config);

  Task UpdatePlannerSettings(PlannerSettings plannerSettings);
}
