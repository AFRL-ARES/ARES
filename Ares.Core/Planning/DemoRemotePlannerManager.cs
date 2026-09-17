using Ares.Core.Execution.VersionChecking;
using Ares.Core.Notifications;
using Ares.Datamodel.Planning;

namespace Ares.Core.Planning;

/// <summary>
/// Demo-mode implementation of <see cref=\"IRemotePlannerManager\"/>.
/// This manager operates entirely in-memory and does not touch the
/// configuration database or planner caches.
/// </summary>
public class DemoRemotePlannerManager : IRemotePlannerManager
{
  private readonly IPlannerServiceRepo _plannerRepo;
  private readonly INotificationHandler _notificationHandler;
  private readonly IDatamodelVersionValidator _versionValidator;
  private readonly IPlannerServiceCache _plannerCache;
  private RemotePlannerMonitor? _remotePlannerMonitor;

  private readonly string _demoPlannerUniqueId = "4b14d5e9-1c9f-4f01-8b2b-4d4d1e2e3e4e";

  public DemoRemotePlannerManager(
    IPlannerServiceRepo plannerRepo,
    INotificationHandler notificationHandler,
    IDatamodelVersionValidator versionValidator,
    IPlannerServiceCache plannerCache)
  {
    _plannerRepo = plannerRepo;
    _notificationHandler = notificationHandler;
    _versionValidator = versionValidator;
    _plannerCache = plannerCache;
  }

  public async Task LoadPlanners()
  {
    // In demo mode we only ensure that the static demo planner exists.
    var existingDemoPlanner = _plannerRepo.GetPlannerById(_demoPlannerUniqueId);
    if(existingDemoPlanner is null)
    {
      // Default demo planner endpoint from DemoRemotePlanner launch settings.
      var demoUrl = "http://localhost:5036";
      await CreateDemoPlanner(demoUrl);
    }
  }

  public Task CreatePlanner(string name, string url)
  {
    // In demo mode, creating additional planners is allowed but purely in-memory.
    var config = new PlannerConfig { UniqueId = Guid.NewGuid().ToString(), Name = name, Url = url };
    var planner = ConfigToPlanner(config);

    if(planner is not null)
    {
      _remotePlannerMonitor = new RemotePlannerMonitor(planner, _plannerCache);
      _plannerRepo.AddPlanner(planner);
    }

    return Task.CompletedTask;
  }

  public Task CreateDemoPlanner(string url)
  {
    var config = new PlannerConfig { UniqueId = _demoPlannerUniqueId, Name = "Demo Remote Planner", Url = url };
    var planner = ConfigToPlanner(config);

    if(planner is not null)
    {
      _plannerRepo.AddPlanner(planner);
      _remotePlannerMonitor = new RemotePlannerMonitor(planner, _plannerCache);
    }

    return Task.CompletedTask;
  }

  public Task RemovePlanner(string plannerId)
  {
    _plannerRepo.RemovePlanner(plannerId);
    return Task.CompletedTask;
  }

  public Task UpdatePlanner(PlannerConfig config)
  {
    var existing = _plannerRepo.GetPlannerById(config.UniqueId);
    if(existing is not null)
    {
      _plannerRepo.RemovePlanner(config.UniqueId);
      var updated = ConfigToPlanner(config);
      if(updated is not null)
      {
        _plannerRepo.AddPlanner(updated);
      }
    }

    return Task.CompletedTask;
  }

  public Task UpdatePlannerSettings(PlannerSettings plannerSettings)
  {
    var planner = _plannerRepo.GetPlannerById(plannerSettings.PlannerId);
    if(planner is null)
      return Task.CompletedTask;

    planner.UpdateSettings(plannerSettings.Settings);
    // No persistence in demo mode.
    return Task.CompletedTask;
  }

  private RemotePlannerService? ConfigToPlanner(PlannerConfig config)
  {
    var uriValid = Uri.TryCreate(config.Url, UriKind.Absolute, out var uri);
    if(!uriValid || uri is null)
    {
      _ = _notificationHandler.HandleNotification(
        "Planner Load Error",
        $"Failed to load a remote planner {config.Name} because the url {config.Url} is invalid.",
        NotificationSeverityEnum.Danger);
      return null;
    }

    return new RemotePlannerService(config.Name, uri, config.UniqueId, _versionValidator);
  }
}
