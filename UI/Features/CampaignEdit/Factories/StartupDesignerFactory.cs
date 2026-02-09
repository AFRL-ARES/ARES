using Ares.Datamodel.Templates;
using Ares.Services;
using Radzen;
using UI.Backend.ViewModels.Automation.CampaignEdit;

namespace UI.Features.CampaignEdit.Factories;

public class StartupDesignerFactory 
{
  private readonly AresAutomation.AresAutomationClient _automationClient;
  private readonly StepDesignerFactory _stepDesignerFactory;
  private readonly AresValidation.AresValidationClient _validationClient;
  private readonly NotificationService _notificationService;

  public StartupDesignerFactory(StepDesignerFactory stepDesignerFactory,
    AresAutomation.AresAutomationClient automationClient,
    AresValidation.AresValidationClient validationClient,
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
