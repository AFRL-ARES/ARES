using Ares.Messaging;
using UI.Backend.ViewModels.Automation.CampaignEdit;

namespace UI.Backend.ViewModels.Factories;

public class ExperimentDesignerFactory
{
  private readonly AresAutomation.AresAutomationClient _automationClient;
  private readonly StepDesignerFactory _stepDesignerFactory;
  private readonly AresValidation.AresValidationClient _validationClient;

  public ExperimentDesignerFactory(StepDesignerFactory stepDesignerFactory, AresAutomation.AresAutomationClient automationClient, AresValidation.AresValidationClient validationClient)
  {
    _stepDesignerFactory = stepDesignerFactory;
    _automationClient = automationClient;
    _validationClient = validationClient;
  }

  public ExperimentDesignerViewModel Create() => new(_stepDesignerFactory, _automationClient, _validationClient);

  public ExperimentDesignerViewModel Create(ExperimentTemplate? existingTemplate)
  {
    if (existingTemplate is null)
      return Create();

    return new ExperimentDesignerViewModel(existingTemplate, _stepDesignerFactory, _automationClient, _validationClient);
  }
}
