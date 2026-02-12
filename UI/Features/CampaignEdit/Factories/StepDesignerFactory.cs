using Ares.Datamodel.Templates;
using UI.Features.CampaignEdit.ViewModels;

namespace UI.Features.CampaignEdit.Factories;

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
