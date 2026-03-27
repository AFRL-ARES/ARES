using Ares.Datamodel.Planning;
using Ares.Services;
using Ares.Core.Grpc.Services;
using Google.Protobuf.WellKnownTypes;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using UI.Application.Notifications;
using System.Reactive.Linq;
using System.Collections.ObjectModel;


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
    SettingsViewModels = [];
  }

  public PlannerConfigEditViewModel GetNewConfigEditViewModel() => new(_planningService);

  public async Task UpdateAvailablePlanners()
  {
    IsLoading = true;

    try 
    {
      var response = await _planningService.GetAllPlanners(new Empty(), null);
      UpdateViewModels(response.Planners);
    }
    catch(Exception ex)
    {
      PushNotification(new AresNotification
      {
        Title = "Error fetching planners",
        Message = ex.Message,
        NotificationSeverity = Severity.Error
      });
    }
    finally
    {
      IsLoading = false;
    }
  }

  private void UpdateViewModels(IEnumerable<PlannerServiceInfo> plannerAdapters)
  {
    var filteredAdapters = plannerAdapters.Where(planner => planner.Name != "Manual Planner");

    SettingsViewModels = filteredAdapters
        .Select(info => new PlannerSettingsViewModel(_planningService, _notificationService, info, OnPlannerRemoved))
        .ToList();
  }

  public async Task AddNewPlanner(PlannerServiceInfo plannerAdapter)
  {
    var request = new AddPlannerRequest() { Name = plannerAdapter.Name, Address = plannerAdapter.Address };
    await _planningService.AddPlanner(request, null);
    await UpdateAvailablePlanners();
  }

  private async Task OnPlannerRemoved()
  {
    await UpdateAvailablePlanners();
  }

  public void PushNotification(AresNotification notification) => _notificationService.PushNotification(notification);

  [Reactive]
  public partial IEnumerable<PlannerSettingsViewModel> SettingsViewModels { get; private set; }

  [Reactive]
  public partial bool IsLoading { get; private set; }
}


