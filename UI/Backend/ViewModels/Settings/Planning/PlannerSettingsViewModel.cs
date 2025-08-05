using Ares.Messaging;
using Ares.Messaging.Planning;
using Grpc.Core;
using ReactiveUI;
using UI.Services.Notification;


namespace UI.Backend.ViewModels.Settings.Planning;

public class PlannerSettingsViewModel : ReactiveObject
{
  private readonly AresPlanning.AresPlanningClient _planningClient;
  private readonly INotificationReceivingService _notificationService;

  public PlannerSettingsViewModel(AresPlanning.AresPlanningClient planningClient,
    INotificationReceivingService notificationService,
    PlannerAdapterInfo genericAdapter,
    Func<Task> onRemoveCallback)
  {
    _planningClient = planningClient;
    _notificationService = notificationService;
    PlannerAdapter = genericAdapter;
    EditViewModel = new PlannerConfigEditViewModel(planningClient, PlannerAdapter);
    OnRemoveCallback = onRemoveCallback;
    IsEditable = !(PlannerAdapter.AdapterName == "Demo Planner" || PlannerAdapter.AdapterName == "Print Planner");
  }

  public PlannerAdapterInfo PlannerAdapter { get; }

  public Func<Task> OnRemoveCallback { get; }

  public PlannerConfigEditViewModel EditViewModel { get; }

  public bool IsEditable { get; }

  public async Task Save()
  {
    var planner = EditViewModel.Save();
    var request = new GenericPlanner();
    request.Name = planner.AdapterName;
    request.Address = planner.Address;

    await _planningClient.UpdatePlannerAsync(request);
    await OnRemoveCallback();
  }

  public async Task Remove()
  {
    var request = new GenericPlanner();
    request.Name = PlannerAdapter.AdapterName;
    request.Address = PlannerAdapter.Address;

    await _planningClient.RemovePlannerAsync(request);
    await OnRemoveCallback();
  }

  public Task Activate()
    => _planningClient.ActivatePlannerAsync(new PlannerActivationRequest
    {
      AdapterName = PlannerAdapter.AdapterName
    }).ResponseAsync;

  public Task<PlannerStatus> GetPlannerStatus()
  {
    try
    {
      return _planningClient.GetPlannerStatusAsync(new PlannerStatusRequest { AdapterName = PlannerAdapter.AdapterName }).ResponseAsync;
    }

    catch(RpcException)
    {
      return Task.FromResult(new PlannerStatus { PlannerState = PlannerState.Error, Message = $"Unable to find a registered Planner with a name {PlannerAdapter.AdapterName}" });
    }
  }

  public void PushNotification(AresNotification notification) => _notificationService.PushNotification(notification);
}
