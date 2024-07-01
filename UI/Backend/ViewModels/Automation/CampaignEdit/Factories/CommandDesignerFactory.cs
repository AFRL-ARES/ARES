using Ares.Messaging;
using UI.Backend.ViewModels.Automation.CampaignEdit;

namespace UI.Backend.ViewModels.Factories;

public class CommandDesignerFactory
{
  private readonly CommandParameterDesignerFactory _commandParameterDesignerFactory;
  private readonly MetadataPickerFactory _metadataPickerFactory;

  public CommandDesignerFactory(MetadataPickerFactory metadataPickerFactory, CommandParameterDesignerFactory commandParameterDesignerFactory)
  {
    _metadataPickerFactory = metadataPickerFactory;
    _commandParameterDesignerFactory = commandParameterDesignerFactory;
  }

  public CommandDesignerViewModel Create()
    => new CommandDesignerViewModel(_commandParameterDesignerFactory, _metadataPickerFactory);

  public CommandDesignerViewModel Create(CommandTemplate existingTemplate)
    => new CommandDesignerViewModel(existingTemplate, _commandParameterDesignerFactory, _metadataPickerFactory);
}
