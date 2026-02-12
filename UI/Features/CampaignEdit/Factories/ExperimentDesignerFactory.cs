using Ares.Datamodel.Templates;
using Ares.Services;
using Radzen;
using ExperimentDesignerViewModel=UI.Features.CampaignEdit.ViewModels.ExperimentDesignerViewModel;

namespace UI.Features.CampaignEdit.Factories;

public class ExperimentDesignerFactory
{
  private readonly AresAutomation.AresAutomationClient _automationClient;
  private readonly StepDesignerFactory _stepDesignerFactory;
  private readonly AresValidation.AresValidationClient _validationClient;
  private readonly NotificationService _notificationService;

  public ExperimentDesignerFactory(StepDesignerFactory stepDesignerFactory,
    AresAutomation.AresAutomationClient automationClient,
    AresValidation.AresValidationClient validationClient,
    NotificationService notificationService)
  {
    _stepDesignerFactory = stepDesignerFactory;
    _automationClient = automationClient;
    _validationClient = validationClient;
    _notificationService = notificationService;
  }

  public ExperimentDesignerViewModel Create() => new(_stepDesignerFactory, _automationClient, _validationClient, _notificationService);

  public ExperimentDesignerViewModel Create(ExperimentTemplate? existingTemplate)
  {
    if(existingTemplate is null)
      return Create();

    return new ExperimentDesignerViewModel(existingTemplate, _stepDesignerFactory, _automationClient, _validationClient, _notificationService);
  }
}
