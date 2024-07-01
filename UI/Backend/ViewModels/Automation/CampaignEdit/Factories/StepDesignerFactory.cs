using Ares.Messaging;
using UI.Backend.ViewModels.Automation.CampaignEdit;

namespace UI.Backend.ViewModels.Factories;

public class StepDesignerFactory
{
  private readonly CommandDesignerFactory _commandDesignerFactory;

  public StepDesignerFactory(CommandDesignerFactory commandDesignerFactory)
  {
    _commandDesignerFactory = commandDesignerFactory;
  }

  public StepDesignerViewModel Create()
    => new StepDesignerViewModel(_commandDesignerFactory);

  public StepDesignerViewModel Create(StepTemplate existingTemplate)
    => new StepDesignerViewModel(existingTemplate, _commandDesignerFactory);
}
