using Ares.Datamodel.Templates;
using Ares.Services;
using Ares.Core.Grpc.Services;
using Google.Protobuf.WellKnownTypes;
using UI.Features.CampaignEdit.ViewModels;
using UI.Application.Notifications;


namespace UI.Features.CampaignEdit.Factories;

public class PlanningDesignerFactory
{
  private readonly PlannerService _client;
  private readonly INotificationReceivingService _notificationService;

  public PlanningDesignerFactory(PlannerService client, INotificationReceivingService notificationService)
  {
    _client = client;
    _notificationService = notificationService;
  }

  public async Task<PlanningViewModel> Create(CampaignTemplate template)
  {
    var planners = await _client.GetAllPlanners(new Empty(), null);
    return new PlanningViewModel(template, planners.Planners, _client, _notificationService);
  }
}


