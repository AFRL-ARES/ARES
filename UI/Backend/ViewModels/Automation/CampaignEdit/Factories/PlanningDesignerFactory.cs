using Ares.Datamodel.Templates;
using Ares.Services;
using Google.Protobuf.WellKnownTypes;
using UI.Services.Notification;


namespace UI.Backend.ViewModels.Automation.CampaignEdit.Factories;

public class PlanningDesignerFactory
{
  private readonly AresPlannerManagementService.AresPlannerManagementServiceClient _client;
  private readonly INotificationReceivingService _notificationService;

  public PlanningDesignerFactory(AresPlannerManagementService.AresPlannerManagementServiceClient client, INotificationReceivingService notificationService)
  {
    _client = client;
    _notificationService = notificationService;
  }

  public async Task<PlanningViewModel> Create(CampaignTemplate template)
  {
    var planners = await _client.GetAllPlannersAsync(new Empty());
    return new PlanningViewModel(template, planners.Planners, _client, _notificationService);
  }
}
