using Ares.Datamodel.Templates;
using Ares.Services;
using Ares.Core.Grpc.Services;
using Radzen;
using CloseoutDesignerViewModel=UI.Features.CampaignEdit.ViewModels.CloseoutDesignerViewModel;

namespace UI.Features.CampaignEdit.Factories;

public class CloseoutDesignerFactory
{
  private readonly AutomationService _automationClient;
  private readonly StepDesignerFactory _stepDesignerFactory;
  private readonly ValidationService _validationClient;
  private readonly NotificationService _notificationService;

  public CloseoutDesignerFactory(StepDesignerFactory stepDesignerFactory,
    AutomationService automationClient,
    ValidationService validationClient,
    NotificationService notificationService)
  {
    _automationClient = automationClient;
    _stepDesignerFactory = stepDesignerFactory;
    _validationClient = validationClient;
    _notificationService = notificationService;
  }

  public CloseoutDesignerViewModel Create() => new(_stepDesignerFactory, _automationClient, _validationClient, _notificationService);

  public CloseoutDesignerViewModel Create(ExperimentTemplate? existingTemplate)
  {
    if(existingTemplate is null)
      return Create();

    return new CloseoutDesignerViewModel(existingTemplate, _stepDesignerFactory, _automationClient, _validationClient, _notificationService);
  }
}
