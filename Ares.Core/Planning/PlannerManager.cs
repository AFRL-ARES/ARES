using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;

namespace Ares.Core.Planning;

public class PlannerManager : IPlannerManager
{
  private readonly IList<IPlanner> _plannerStore = new List<IPlanner>();
  private readonly IDbContextFactory<CoreDatabaseContext> _dbContextFactory;

  public PlannerManager(IDbContextFactory<CoreDatabaseContext> dbContextFactory)
  {
    _dbContextFactory = dbContextFactory;
    var manualPlanner = new ManualPlanner();
    RegisterPlanner(manualPlanner);
  }

  public async Task Init()
  {
    using var context = await _dbContextFactory.CreateDbContextAsync();
    var availablePlanners = context.Planners;

    foreach(var info in availablePlanners)
    {
      var planner = new AresPlanner.AresPlanner(info.AdapterName, new Uri(info.Address))
      {
        UniqueId = info.UniqueId
      };

      await planner.Init();
      await RegisterPlanner(planner);
    }
  }

  public T GetPlanner<T>(Version version) where T : IPlanner
  {
    var typedPlanners = _plannerStore.OfType<T>().ToArray();
    if(!typedPlanners.Any())
      throw new KeyNotFoundException($"Unable to find any planners of type {typeof(T).Name} in the registry.");

    var planner = typedPlanners.FirstOrDefault(p => p.Version == version);
    if(planner is null)
      throw new KeyNotFoundException($"Unable to find planner {typeof(T).Name} with version {version} in the registry.");

    return planner;
  }

  public T GetPlanner<T>(string name, Version version) where T : IPlanner
  {
    var typedPlanners = _plannerStore.OfType<T>().ToArray();
    if(!typedPlanners.Any())
      throw new KeyNotFoundException($"Unable to find any planners of type {typeof(T).Name} in the registry.");

    var versionedPlanners = typedPlanners.Where(p => p.Name == name);
    if(versionedPlanners is null)
      throw new KeyNotFoundException($"Unable to find planner of type {typeof(T).Name} named {name} in the registry.");

    var planner = versionedPlanners.FirstOrDefault(p => p.Version == version);
    if(planner is null)
      throw new KeyNotFoundException($"Unable to find planner of type {typeof(T).Name} named {name} with version {version} in the registry.");

    return planner;
  }

  public IPlanner GetPlanner(string type)
  {
    var typedPlanners = _plannerStore.Where(p => p.GetType().Name == type).ToList();
    if(!typedPlanners.Any())
      throw new KeyNotFoundException($"Unable to find any planners of type {type} in the registry.");

    return typedPlanners.OrderByDescending(planner => planner.Version).First();
  }

  public IPlanner GetPlanner(string type, Version version)
  {
    var typedPlanners = _plannerStore.Where(p => p.GetType().Name == type).ToArray();
    if(!typedPlanners.Any())
      throw new KeyNotFoundException($"Unable to find any planners of type {type} in the registry.");

    var planner = typedPlanners.FirstOrDefault(p => p.Version == version);
    if(planner is null)
      throw new KeyNotFoundException($"Unable to find planner {type} with version {version} in the registry.");

    return planner;
  }

  public IPlanner GetPlanner(string type, string name, Version version)
  {
    var typedPlanners = _plannerStore.Where(p => p.GetType().Name == type).ToArray();
    if(!typedPlanners.Any())
      throw new KeyNotFoundException($"Unable to find any planners of type {type} in the registry.");

    var versionedPlanners = typedPlanners.Where(p => p.Name == name);
    if(versionedPlanners is null)
      throw new KeyNotFoundException($"Unable to find planner of type {type} named {name} in the registry.");

    var planner = versionedPlanners.FirstOrDefault(p => p.Version == version);
    if(planner is null)
      throw new KeyNotFoundException($"Unable to find planner of type {type} named {name} with version {version} in the registry.");

    return planner;
  }

  public IPlanner GetPlanner(string type, string name)
  {
    var typedPlanners = _plannerStore.Where(p => p.GetType().Name == type).ToArray();
    if(!typedPlanners.Any())
      throw new KeyNotFoundException($"Unable to find any planners of type {type} in the registry.");

    var versionedPlanners = typedPlanners.Where(p => p.Name == name).ToList();
    if(versionedPlanners is null)
      throw new KeyNotFoundException($"Unable to find planner of type {type} named {name} in the registry.");

    return versionedPlanners.OrderByDescending(planner => planner.Version).First();
  }

  public T GetPlanner<T>() where T : IPlanner
  {
    var typedPlanners = _plannerStore.OfType<T>().ToList();
    if(!typedPlanners.Any())
      throw new KeyNotFoundException($"Unable to find any planners of type {typeof(T).Name} in the registry.");

    return typedPlanners.OrderByDescending(planner => planner.Version).First();
  }

  public IPlanner? GetPlannerByName(string name) => _plannerStore.FirstOrDefault(planner => planner.Name == name);

  public Task RegisterPlanner(IPlanner planner)
  {
    var plannerExists = _plannerStore.Any(p => p == planner || (p.Name == planner.Name && p.Version == planner.Version && planner.GetType() == p.GetType()));
    if(plannerExists)
      return Task.CompletedTask;

    _plannerStore.Add(planner);
    return Task.CompletedTask;
  }

  public Task UnregisterPlanner(IPlanner planner)
  {
    var plannerExists = _plannerStore.Any(p => p == planner || (p.Name == planner.Name && p.Version == planner.Version && planner.GetType() == p.GetType()));
    if(!plannerExists)
      return Task.CompletedTask;

    _plannerStore.Remove(planner);
    return Task.CompletedTask;
  }

  public IEnumerable<IPlanner> AvailablePlanners => new ReadOnlyCollection<IPlanner>(_plannerStore);
}
