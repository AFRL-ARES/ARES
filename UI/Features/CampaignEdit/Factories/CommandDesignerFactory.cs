using Ares.Core.CustomCommands;
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
  private readonly ICustomCommandPersistenceService _customCommandPersistenceService;

  public CommandDesignerFactory(
    MetadataPickerFactory metadataPickerFactory,
    CommandParameterDesignerFactory commandParameterDesignerFactory,
    DevicesService devicesClient,
    ICustomCommandPersistenceService customCommandPersistenceService)
  {
    _metadataPickerFactory = metadataPickerFactory;
    _commandParameterDesignerFactory = commandParameterDesignerFactory;
    _devicesClient = devicesClient;
    _customCommandPersistenceService = customCommandPersistenceService;
  }

  public CommandDesignerViewModel Create()
    => new CommandDesignerViewModel(
      _commandParameterDesignerFactory,
      _metadataPickerFactory,
      _devicesClient,
      _customCommandPersistenceService);

  public CommandDesignerViewModel Create(CommandTemplate existingTemplate)
    => new CommandDesignerViewModel(
      existingTemplate,
      _commandParameterDesignerFactory,
      _metadataPickerFactory,
      _devicesClient,
      _customCommandPersistenceService);
}
