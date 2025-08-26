using System.Collections.ObjectModel;

namespace Ares.Core.Planning;

public class PlannerServiceRepo : IPlannerServiceRepo
{
  private readonly IList<IPlannerService> _plannerStore = [];

  public PlannerServiceRepo()
  {
    var defaultPlanner = new NonePlannerService();
    var manualPlanner = new ManualPlanner();
    AddPlanner(defaultPlanner);
    AddPlanner(manualPlanner);
  }

  public IPlannerService GetDefaultPlanner()
  {
    return _plannerStore.OfType<NonePlannerService>().First();
  }

  public ManualPlanner GetManualPlanner()
  {
    return _plannerStore.OfType<ManualPlanner>().First();
  }

  public IPlannerService? GetPlannerByName(string name)
  {
    var planner = _plannerStore.FirstOrDefault(planner => planner.Name == name);
    return planner;
  }

  public void AddPlanner(IPlannerService planner)
  {
    var plannerExists = _plannerStore.Any(p => p == planner || (p.Name == planner.Name && p.Version == planner.Version && planner.Type == p.Type));

    if(plannerExists)
      throw new InvalidOperationException($"Planner {planner.Name}{planner.Version} of type {planner.GetType().Name} already registered!");

    _plannerStore.Add(planner);
  }

  public void RemovePlanner(IPlannerService planner)
  {
    var plannerExists = _plannerStore.Any(p => p == planner || (p.Name == planner.Name && p.Version == planner.Version && planner.Type == p.Type));
    if(!plannerExists)
      return;

    _plannerStore.Remove(planner);
  }

  public IPlannerService? GetPlannerById(string id)
  {
    var planner = _plannerStore.FirstOrDefault(planner => planner.UniqueId == id);
    return planner;
  }

  public void RemovePlanner(string plannerId)
  {
    var planner = _plannerStore.FirstOrDefault(planner => planner.UniqueId ==  plannerId);
    if(planner is null)
      return;

    _plannerStore.Remove(planner);
  }

  public IEnumerable<IPlannerService> AvailablePlannerServices => new ReadOnlyCollection<IPlannerService>(_plannerStore);
}
