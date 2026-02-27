using Ares.Datamodel.Connection;
using Ares.Datamodel.Planning;
using Ares.Services;
using Ares.Core.Grpc.Services;
using Grpc.Core;
using ReactiveUI;
using UI.Application.Notifications;


namespace UI.Features.Planning.Settings;

public class PlannerSettingsViewModel : ReactiveObject
{
  private readonly PlannerService _planningClient;
  private readonly INotificationReceivingService _notificationService;

  public PlannerSettingsViewModel(PlannerService planningClient,
    INotificationReceivingService notificationService,
    PlannerServiceInfo genericAdapter,
    Func<Task> onRemoveCallback)
  {
    _planningClient = planningClient;
    _notificationService = notificationService;
    PlannerAdapter = genericAdapter;
    EditViewModel = new PlannerConfigEditViewModel(planningClient, PlannerAdapter);
    SettingsEditorViewModel = new PlannerSettingsEditorViewModel(planningClient, PlannerAdapter);
    OnRemoveCallback = onRemoveCallback;
  }

  public PlannerServiceInfo PlannerAdapter { get; }

  public Func<Task> OnRemoveCallback { get; }

  public PlannerConfigEditViewModel EditViewModel { get; }

  public PlannerSettingsEditorViewModel SettingsEditorViewModel { get; }

  public async Task Save()
  {
    var planner = EditViewModel.Save();
    var request = new UpdatePlannerRequest();
    request.Name = planner.Name;
    request.Url = planner.Address;
    request.PlannerId = PlannerAdapter.UniqueId;

    await _planningClient.UpdatePlanner(request, null);
    await OnRemoveCallback();
  }

  public async Task Remove()
  {
    var request = new RemovePlannerRequest();
    request.PlannerId = PlannerAdapter.UniqueId;

    await _planningClient.RemovePlanner(request, null);
    await OnRemoveCallback();
  }

  public async Task<StateResponse> GetPlannerStatus()
  {
    try
    {
      return await _planningClient.GetState(new StateRequest { Id = PlannerAdapter.UniqueId }, null);
    }

    catch(Exception)
    {
      return new StateResponse 
      { 
        State = State.Inactive, 
        StateMessage = $"Unable to find a registered Planner with a name {PlannerAdapter.Name}" 
      };
    }
  }

  public void PushNotification(AresNotification notification) => _notificationService.PushNotification(notification);
}


