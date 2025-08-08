using Ares.Datamodel;
using Ares.Messaging.Planning;
using Ares.Services;
using Google.Protobuf.WellKnownTypes;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using UI.Services.Notification;


namespace UI.Backend.ViewModels.Settings.Planning;

public class PlannerSettingsListViewModel : ReactiveObject
{
  private readonly AresPlanning.AresPlanningClient _planningService;
  private readonly INotificationReceivingService _notificationService;

  public PlannerSettingsListViewModel(AresPlanning.AresPlanningClient planningService,
    INotificationReceivingService notificationService)
  {
    _planningService = planningService;
    _notificationService = notificationService;
    UpdateAvailablePlanners();
  }

  public PlannerConfigEditViewModel GetNewConfigEditViewModel() => new(_planningService);

  public Task UpdateAvailablePlanners()
  {
    SettingsViewModels = null;
    return _planningService
      .GetAllPlannersAsync(new Empty())
      .ResponseAsync.ContinueWith(task => UpdateViewModels(task.Result.Planners));
  }

  private void UpdateViewModels(IEnumerable<PlannerAdapterInfo> plannerAdapters)
  {
    plannerAdapters = plannerAdapters.Where(planner => planner.AdapterName != "Manual Planner");
    var viewModels = plannerAdapters.Select(info => new PlannerSettingsViewModel(_planningService, _notificationService, info, OnPlannerRemoved)).ToList();
    SettingsViewModels = viewModels;
  }

  public async Task AddNewPlanner(PlannerAdapterInfo plannerAdapter)
  {
    var request = new GenericPlanner() { Name = plannerAdapter.AdapterName, Address = plannerAdapter.Address };
    await _planningService.AddPlannerAsync(request);
    await UpdateAvailablePlanners();
  }

  private async Task OnPlannerRemoved()
  {
    SettingsViewModels = null;
    await UpdateAvailablePlanners();
  }
  public void PushNotification(AresNotification notification) => _notificationService.PushNotification(notification);

  [Reactive]
  public IEnumerable<PlannerSettingsViewModel>? SettingsViewModels { get; private set; }
}
