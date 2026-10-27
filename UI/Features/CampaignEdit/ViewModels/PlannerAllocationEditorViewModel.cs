using Ares.Datamodel.Planning;
using Ares.Datamodel.Templates;
using Ares.Services;
using Ares.Core.Grpc.Services;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using UI.Application.Notifications;
using UI.Features.Planning.Settings;

namespace UI.Features.CampaignEdit.ViewModels;

public partial class PlannerAllocationEditorViewModel : ReactiveObject
{
  private readonly PlannerService _plannerClient;
  private readonly IAresUINotificationService _notificationService;
  private PlannerServiceInfo? _selectedService;

  public PlannerAllocationEditorViewModel(ParameterMetadata metadata,
    PlannerServiceInfo? plannerInfo,
    IEnumerable<PlannerServiceInfo> plannerAdapters,
    PlannerService plannerClient,
    IAresUINotificationService notificationService)
  {
    var plannerArray = plannerAdapters.ToArray();

    ParameterMetadata = metadata;
    PlannerServices = plannerArray;
    _plannerClient = plannerClient;
    _notificationService = notificationService;

    // Initialize to empty by default
    PlannerOptions = Enumerable.Empty<Planner>();

    if(plannerInfo is not null)
    {
      // Match against the fresh live adapter collection instead of using the stale deserialized snapshot
      var liveService = plannerArray.FirstOrDefault(p => p.UniqueId == plannerInfo.UniqueId || p.Name == plannerInfo.Name);

      SelectedService = liveService ?? plannerInfo;

      // Restore specific planner option selection if specified in metadata
      if(!string.IsNullOrEmpty(metadata.PlannerName))
      {
        var matchedOption = PlannerOptions.FirstOrDefault(p => p.PlannerName == metadata.PlannerName);
        if(matchedOption != null)
        {
          SelectedPlannerOption = matchedOption;
        }
      }
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

    allocation.Parameter.PlannerName = SelectedPlannerOption?.PlannerName ?? SelectedService.Name;
    allocation.Parameter.PlannerDescription = SelectedPlannerOption?.Description ?? SelectedService.Description;

    return allocation;
  }

  public async Task SaveSettingsChanges()
  {
    if(EditViewModel is null)
      throw new NullReferenceException("Edit View Model was null, settings cannot be saved");

    var planner = EditViewModel.Save();
    var request = new UpdatePlannerRequest();
    request.Name = planner.Name;
    request.Url = planner.Address;
    request.PlannerId = _selectedService?.UniqueId;

    await _plannerClient.UpdatePlanner(request, null);
  }

  public void PushNotification(AresNotification notification) => _notificationService.PushNotification(notification);

  public async Task UpdatePlannerOptions()
  {
    if(SelectedService is null)
      return;

    var updatedInfo = await _plannerClient.GetInfo(new PlannerInfoRequest { PlannerId = SelectedService.UniqueId }, null);

    if(updatedInfo?.Info is not null && !string.IsNullOrEmpty(updatedInfo.Info.Name))
    {
      // Update the SelectedService reference with the fresh gRPC response
      SelectedService = updatedInfo.Info;
    }
    else
    {
      var notification = new AresNotification
      {
        Title = "Assigned Planner Unavailable!",
        Message = "This template uses a planner that ARES no longer has a connection with. The template won't be usable until this is resolved.",
        NotificationSeverity = Severity.Warning,
        Loiter = true
      };

      _notificationService.PushNotification(notification);
    }
  }

  public PlannerServiceInfo? SelectedService
  {
    get => _selectedService;
    set
    {
      this.RaiseAndSetIfChanged(ref _selectedService, value);

      PlannerOptions = _selectedService?.Capabilities?.AvailablePlanners?.ToList() ?? new List<Planner>();
      SelectedPlannerOption = PlannerOptions.FirstOrDefault();
      if(_selectedService is not null)
      {
        SettingsViewModel = new PlannerSettingsEditorViewModel(_plannerClient, _selectedService);
        EditViewModel = new PlannerConfigEditViewModel(_plannerClient, _selectedService);
      }
        
    }
  }

  [Reactive]
  public partial ParameterMetadata ParameterMetadata { get; set; }
  public IEnumerable<PlannerServiceInfo> PlannerServices { get; }
  [Reactive]
  public partial IEnumerable<Planner> PlannerOptions { get; set; }
  [Reactive]
  public partial Planner? SelectedPlannerOption { get; set; }
  public PlannerSettingsEditorViewModel? SettingsViewModel { get; set; }
  public PlannerConfigEditViewModel? EditViewModel { get; set; }
}