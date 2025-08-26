using Ares.Datamodel;
using Ares.Datamodel.Planning;
using Ares.Datamodel.Templates;
using Ares.Services;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using UI.Services.Notification;

namespace UI.Backend.ViewModels.Automation.CampaignEdit;

public class PlannerAllocationEditorViewModel : ReactiveObject
{
  private readonly AresPlannerManagementService.AresPlannerManagementServiceClient _plannerClient;
  private readonly INotificationReceivingService _notificationService;
  private string _selectedPlannerOption = string.Empty;
  private PlannerServiceInfo? _selectedAdapter;

  public PlannerAllocationEditorViewModel(ParameterMetadata metadata,
    PlannerServiceInfo? plannerInfo,
    IEnumerable<PlannerServiceInfo> plannerAdapters,
    AresPlannerManagementService.AresPlannerManagementServiceClient plannerClient,
    INotificationReceivingService notificationService)
  {
    var plannerArray = plannerAdapters.ToArray();

    ParameterMetadata = metadata;
    PlannerServices = plannerArray;
    _plannerClient = plannerClient;
    _notificationService = notificationService;

    if(plannerInfo is not null)
    {
      SelectedService = plannerInfo;
      SelectedPlannerOption = metadata.PlannerName;
    }
  }

  public PlannerAllocation? Save()
  {
    if(SelectedService is null)
      return null;

    var allocation = new PlannerAllocation
    {
      Parameter = ParameterMetadata,
      Planner = SelectedService,
      UniqueId = Guid.NewGuid().ToString()
    };

    var selectedPlannerOption = PlannerOptions.FirstOrDefault(option => option.PlannerName == _selectedPlannerOption);
    allocation.Parameter.PlannerName = _selectedPlannerOption ?? SelectedService.Name;
    allocation.Parameter.PlannerDescription = selectedPlannerOption?.Description ?? SelectedService.Description;

    return allocation;
  }

  public async Task UpdatePlannerOptions()
  {
    if(SelectedService is null)
      return;

    var updatedInfo = await _plannerClient.GetInfoAsync(new PlannerInfoRequest { PlannerId = SelectedService.UniqueId });

    if(updatedInfo.Info.Name != string.Empty)
    {
      PlannerOptions = updatedInfo.Info.Capabilities.AvailablePlanners;

      //Not all adapters will have multiple options, if not auto assign the value
      if(PlannerOptions.Count() <= 1)
        SelectedPlannerOption = SelectedService.Name;
    }

    else
    {
      var notification = new AresNotification();
      notification.Title = "Assigned Planner Unavailable!";
      notification.Message = $"This template uses a planner that ARES no longer has a connection with. The template won't be usable until this is resolved.";
      notification.NotificationSeverity = Severity.Warning;
      notification.Loiter = true;
      _notificationService.PushNotification(notification);
    }
  }

  public async Task UpdatePlannerSettings()
  {
    if(SelectedService is null)
      return;

    PlannerSettings = await _plannerClient.GetPlannerSettingsAsync(new PlannerSettingsRequest { PlannerId = SelectedService.UniqueId });
    PlannerDescription = PlannerOptions.First(p => p.PlannerName == SelectedPlannerOption).Description;
  }

  public PlannerServiceInfo? SelectedService
  {
    get => _selectedAdapter;

    set
    {
      if(value is null || _selectedAdapter == value)
        return;

      if(_selectedAdapter is not null)
        _selectedPlannerOption = string.Empty;

      _selectedAdapter = value;
      _ = UpdatePlannerOptions();
    }
  }

  public string? SelectedPlannerOption
  {
    get => _selectedPlannerOption;

    set
    {
      if(value is null || _selectedPlannerOption == value)
        return;

      _selectedPlannerOption = value;
      _ = UpdatePlannerSettings();
    }
  }

  [Reactive]
  public IEnumerable<Planner> PlannerOptions { get; set; } = Enumerable.Empty<Planner>();
  [Reactive]
  public AresStruct PlannerSettings { get; set; } = new AresStruct();
  [Reactive]
  public string PlannerDescription { get; set; } = string.Empty;
  public ParameterMetadata ParameterMetadata { get; }
  public IEnumerable<PlannerServiceInfo> PlannerServices { get; }

}
