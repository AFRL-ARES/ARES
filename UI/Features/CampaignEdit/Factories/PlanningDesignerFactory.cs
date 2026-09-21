using Ares.Datamodel.Templates;
using Ares.Core.Grpc.Services;
using Google.Protobuf.WellKnownTypes;
using UI.Features.CampaignEdit.ViewModels;
using UI.Application.Notifications;
using Ares.Core.Analyzing;


namespace UI.Features.CampaignEdit.Factories;

public class PlanningDesignerFactory
{
  private readonly PlannerService _client;
  private readonly IUiNotificationService _notificationService;
  private readonly IAnalyzerRepo _analyzerRepo;

  public PlanningDesignerFactory(PlannerService client, IUiNotificationService notificationService, IAnalyzerRepo analyzerRepo)
  {
    _client = client;
    _notificationService = notificationService;
    _analyzerRepo = analyzerRepo;
  }

  public async Task<PlanningViewModel> Create(CampaignTemplate template)
  {
    var matchingAnalyzer = _analyzerRepo.GetAnalyzerById(template.ExperimentTemplate.AnalyzerId);

    var planners = await _client.GetAllPlanners(new Empty(), null);
    return new PlanningViewModel(template, planners.Planners, _client, _notificationService, matchingAnalyzer);
  }
}


