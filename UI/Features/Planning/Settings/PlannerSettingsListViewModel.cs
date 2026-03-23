using Ares.Datamodel.Planning;
using Ares.Services;
using Ares.Core.Grpc.Services;
using Google.Protobuf.WellKnownTypes;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using UI.Application.Notifications;


namespace UI.Features.Planning.Settings;

public partial class PlannerSettingsListViewModel : ReactiveObject
{
  private readonly PlannerService _planningService;
  private readonly INotificationReceivingService _notificationService;

  public PlannerSettingsListViewModel(PlannerService planningService,
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
      .GetAllPlanners(new Empty(), null)
      .ContinueWith(task => { if(task.IsCompletedSuccessfully) UpdateViewModels(task.Result.Planners); });
  }

  private void UpdateViewModels(IEnumerable<PlannerServiceInfo> plannerAdapters)
  {
    plannerAdapters = plannerAdapters.Where(planner => planner.Name != "Manual Planner");
    var viewModels = plannerAdapters.Select(info => new PlannerSettingsViewModel(_planningService, _notificationService, info, OnPlannerRemoved)).ToList();
    SettingsViewModels = viewModels;
  }

  public async Task AddNewPlanner(PlannerServiceInfo plannerAdapter)
  {
    var request = new AddPlannerRequest() { Name = plannerAdapter.Name, Address = plannerAdapter.Address };
    await _planningService.AddPlanner(request, null);
    await UpdateAvailablePlanners();
  }

  private async Task OnPlannerRemoved()
  {
    SettingsViewModels = null;
    await UpdateAvailablePlanners();
  }
  public void PushNotification(AresNotification notification) => _notificationService.PushNotification(notification);

  [Reactive]
  public partial IEnumerable<PlannerSettingsViewModel>? SettingsViewModels { get; private set; }
}


