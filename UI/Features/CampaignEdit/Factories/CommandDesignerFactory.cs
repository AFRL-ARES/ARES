using Ares.Datamodel.Templates;
using Ares.Services.Device;
using Ares.Core.Grpc.Services;
using CommandDesignerViewModel=UI.Features.CampaignEdit.ViewModels.CommandDesignerViewModel;

namespace UI.Features.CampaignEdit.Factories;

public class CommandDesignerFactory
{
  private readonly CommandParameterDesignerFactory _commandParameterDesignerFactory;
  private readonly DevicesService _devicesClient;
  private readonly MetadataPickerFactory _metadataPickerFactory;

  public CommandDesignerFactory(
    MetadataPickerFactory metadataPickerFactory,
    CommandParameterDesignerFactory commandParameterDesignerFactory,
    DevicesService devicesClient)
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
