using System.Collections.ObjectModel;

namespace Ares.Core.Planning;

public class PlannerManager : IPlannerManager
{
  private readonly IList<IPlanner> _plannerStore = new List<IPlanner>();

  public PlannerManager()
  {
    var manualPlanner = new ManualPlanner();
    RegisterPlanner(manualPlanner);
  }

  public T GetPlanner<T>(Version version) where T : IPlanner
  {
    var typedPlanners = _plannerStore.OfType<T>().ToArray();
    if (!typedPlanners.Any())
      throw new KeyNotFoundException($"Unable to find any planners of type {typeof(T).Name} in the registry.");

    var planner = typedPlanners.FirstOrDefault(p => p.Version == version);
    if (planner is null)
      throw new KeyNotFoundException($"Unable to find planner {typeof(T).Name} with version {version} in the registry.");

    return planner;
  }

  public T GetPlanner<T>(string name, Version version) where T : IPlanner
  {
    var typedPlanners = _plannerStore.OfType<T>().ToArray();
    if (!typedPlanners.Any())
      throw new KeyNotFoundException($"Unable to find any planners of type {typeof(T).Name} in the registry.");

    var versionedPlanners = typedPlanners.Where(p => p.Name == name);
    if (versionedPlanners is null)
      throw new KeyNotFoundException($"Unable to find planner of type {typeof(T).Name} named {name} in the registry.");

    var planner = versionedPlanners.FirstOrDefault(p => p.Version == version);
    if (planner is null)
      throw new KeyNotFoundException($"Unable to find planner of type {typeof(T).Name} named {name} with version {version} in the registry.");

    return planner;
  }

  public IPlanner GetPlanner(string type)
  {
    var typedPlanners = _plannerStore.Where(p => p.GetType().Name == type).ToList();
    if (!typedPlanners.Any())
      throw new KeyNotFoundException($"Unable to find any planners of type {type} in the registry.");

    return typedPlanners.OrderByDescending(planner => planner.Version).First();
  }

  public IPlanner GetPlanner(string type, Version version)
  {
    var typedPlanners = _plannerStore.Where(p => p.GetType().Name == type).ToArray();
    if (!typedPlanners.Any())
      throw new KeyNotFoundException($"Unable to find any planners of type {type} in the registry.");

    var planner = typedPlanners.FirstOrDefault(p => p.Version == version);
    if (planner is null)
      throw new KeyNotFoundException($"Unable to find planner {type} with version {version} in the registry.");

    return planner;
  }

  public IPlanner GetPlanner(string type, string name, Version version)
  {
    var typedPlanners = _plannerStore.Where(p => p.GetType().Name == type).ToArray();
    if (!typedPlanners.Any())
      throw new KeyNotFoundException($"Unable to find any planners of type {type} in the registry.");

    var versionedPlanners = typedPlanners.Where(p => p.Name == name);
    if (versionedPlanners is null)
      throw new KeyNotFoundException($"Unable to find planner of type {type} named {name} in the registry.");

    var planner = versionedPlanners.FirstOrDefault(p => p.Version == version);
    if (planner is null)
      throw new KeyNotFoundException($"Unable to find planner of type {type} named {name} with version {version} in the registry.");

    return planner;
  }

  public IPlanner GetPlanner(string type, string name)
  {
    var typedPlanners = _plannerStore.Where(p => p.GetType().Name == type).ToArray();
    if (!typedPlanners.Any())
      throw new KeyNotFoundException($"Unable to find any planners of type {type} in the registry.");

    var versionedPlanners = typedPlanners.Where(p => p.Name == name).ToList();
    if (versionedPlanners is null)
      throw new KeyNotFoundException($"Unable to find planner of type {type} named {name} in the registry.");

    return versionedPlanners.OrderByDescending(planner => planner.Version).First();
  }

  public T GetPlanner<T>() where T : IPlanner
  {
    var typedPlanners = _plannerStore.OfType<T>().ToList();
    if (!typedPlanners.Any())
      throw new KeyNotFoundException($"Unable to find any planners of type {typeof(T).Name} in the registry.");

    return typedPlanners.OrderByDescending(planner => planner.Version).First();
  }

  public void RegisterPlanner(IPlanner planner)
  {
    var plannerExists = _plannerStore.Any(p => p == planner || (p.Name == planner.Name && p.Version == planner.Version && planner.GetType() == p.GetType()));
    if (plannerExists)
      throw new InvalidOperationException($"Planner {planner.Name}{planner.Version} of type {planner.GetType().Name} already registered");

    _plannerStore.Add(planner);
  }

  public IEnumerable<IPlanner> AvailablePlanners => new ReadOnlyCollection<IPlanner>(_plannerStore);
}
