using Ares.Datamodel.Templates;
using Ares.Services;
using Radzen;
using CloseoutDesignerViewModel=UI.Features.CampaignEdit.ViewModels.CloseoutDesignerViewModel;

namespace UI.Features.CampaignEdit.Factories;

public class CloseoutDesignerFactory
{
  private readonly AresAutomation.AresAutomationClient _automationClient;
  private readonly StepDesignerFactory _stepDesignerFactory;
  private readonly AresValidation.AresValidationClient _validationClient;
  private readonly NotificationService _notificationService;

  public CloseoutDesignerFactory(StepDesignerFactory stepDesignerFactory,
    AresAutomation.AresAutomationClient automationClient,
    AresValidation.AresValidationClient validationClient,
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
