using Ares.Datamodel.Templates;
using Ares.Services.Device;
using UI.Backend.ViewModels.Automation.CampaignEdit;

namespace UI.Features.CampaignEdit.Factories;

public class CommandDesignerFactory
{
  private readonly CommandParameterDesignerFactory _commandParameterDesignerFactory;
  private readonly AresDevices.AresDevicesClient _devicesClient;
  private readonly MetadataPickerFactory _metadataPickerFactory;

  public CommandDesignerFactory(
    MetadataPickerFactory metadataPickerFactory,
    CommandParameterDesignerFactory commandParameterDesignerFactory,
    AresDevices.AresDevicesClient devicesClient)
  {
    _metadataPickerFactory = metadataPickerFactory;
    _commandParameterDesignerFactory = commandParameterDesignerFactory;
    _devicesClient = devicesClient;
  }

  public CommandDesignerViewModel Create()
    => new CommandDesignerViewModel(_commandParameterDesignerFactory, _metadataPickerFactory, _devicesClient);

  public CommandDesignerViewModel Create(CommandTemplate existingTemplate)
    => new CommandDesignerViewModel(existingTemplate, _commandParameterDesignerFactory, _metadataPickerFactory, _devicesClient);
}
