using Ares.Datamodel.Templates;
using Ares.Services;
using Ares.Core.Grpc.Services;
using Radzen;
using StartupDesignerViewModel=UI.Features.CampaignEdit.ViewModels.StartupDesignerViewModel;

namespace UI.Features.CampaignEdit.Factories;

public class StartupDesignerFactory 
{
  private readonly AutomationService _automationClient;
  private readonly StepDesignerFactory _stepDesignerFactory;
  private readonly ValidationService _validationClient;
  private readonly NotificationService _notificationService;

  public StartupDesignerFactory(StepDesignerFactory stepDesignerFactory,
    AutomationService automationClient,
    ValidationService validationClient,
    NotificationService notificationService)
  {
    _automationClient = automationClient;
    _stepDesignerFactory = stepDesignerFactory;
    _validationClient = validationClient;
    _notificationService = notificationService;
  }

  public StartupDesignerViewModel Create() => new(_stepDesignerFactory, _automationClient, _validationClient, _notificationService);

  public StartupDesignerViewModel Create(ExperimentTemplate? existingTemplate)
  {
    if(existingTemplate is null)
      return Create();

    return new StartupDesignerViewModel(existingTemplate, _stepDesignerFactory, _automationClient, _validationClient, _notificationService);
  }

}
