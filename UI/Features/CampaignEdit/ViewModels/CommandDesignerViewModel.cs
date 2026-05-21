using Ares.Datamodel;
using Ares.Datamodel.Templates;
using Ares.Services.Device;
using Ares.Core.Grpc.Services;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using UI.Features.CampaignEdit.Factories;

namespace UI.Features.CampaignEdit.ViewModels;

public partial class CommandDesignerViewModel : ReactiveObject
{
  private readonly CommandParameterDesignerFactory _commandParameterDesignerFactory;
  private readonly MetadataPickerFactory _metadataPickerFactory;
  private readonly DevicesService _devicesClient;
  private CommandMetadata? _commandMetadata;
  private CommandTemplate _commandTemplate = null!;

  public CommandDesignerViewModel(
    CommandTemplate existingTemplate,
    CommandParameterDesignerFactory commandParameterDesignerFactory,
    MetadataPickerFactory metadataPickerFactory,
    DevicesService devicesClient)
  {
    ArgumentDesigners = [];
    _commandParameterDesignerFactory = commandParameterDesignerFactory;
    _metadataPickerFactory = metadataPickerFactory;
    _devicesClient = devicesClient;

    CommandTemplate = existingTemplate;
  }

  public CommandDesignerViewModel(CommandParameterDesignerFactory commandParameterDesignerFactory, MetadataPickerFactory metadataPickerFactory, DevicesService devicesClient)
  {
    ArgumentDesigners = [];
    _commandParameterDesignerFactory = commandParameterDesignerFactory;
    _metadataPickerFactory = metadataPickerFactory;
    _devicesClient = devicesClient;

    CommandTemplate = new CommandTemplate
    {
      UniqueId = Guid.NewGuid().ToString()
    };
  }

  public CommandTemplate CommandTemplate
  {
    get => _commandTemplate;

    set
    {
      _commandTemplate = value;
      CommandMetadata = value.Metadata;
      InitTemplate(value);
    }
  }

  public int Index { get; set; }

  public string? TemplateDeviceName { get; private set; }
  public string? MetadataDeviceName { get; private set; }
  public string? TemplateCommandName => CommandTemplate.Metadata?.Name;

  public bool TemplateOutputProvider => CommandTemplate.HasOutputVarName;

  public bool OutputProvider { get; set; }

  public bool HasOutputMetadata => CommandMetadata?.OutputMetadata?.DataSchema is not null;

  public string? OutputVariableName { get; set; }

  public IEnumerable<Parameter> Arguments => CommandTemplate.Parameters;

  public CommandMetadata? CommandMetadata
  {
    get => _commandMetadata;

    set
    {
      _commandMetadata = value;
      InitMetadata(value);
    }
  }

  public MetadataPickerViewModel? MetadataPickerViewModel { get; set; }

  [Reactive]
  public partial IEnumerable<CommandParameterDesignerViewModel> ArgumentDesigners { get; private set; }

  public CommandTemplate Save()
  {
    CommandTemplate.Parameters.Clear();
    CommandTemplate.Parameters.AddRange(ArgumentDesigners.Select(model => model.Save()));
    if(CommandMetadata is not null)
    {
      CommandTemplate.Metadata = CommandMetadata;
      CommandTemplate.Metadata.DeviceType = MetadataDeviceName;
    }
      

    CommandTemplate.Index = Index;
    CommandTemplate.ClearOutputVarName();
    if(OutputProvider && HasOutputMetadata && !string.IsNullOrWhiteSpace(OutputVariableName))
    {
      CommandTemplate.OutputVarName = OutputVariableName.Trim();
    }

    return CommandTemplate;
  }

  private async Task InitTemplate(CommandTemplate existingTemplate)
  {
    Index = Convert.ToInt32(existingTemplate.Index);
    var existingParamDesigners = existingTemplate.Parameters.Select(_commandParameterDesignerFactory.Create).ToArray();
    ArgumentDesigners = [.. existingParamDesigners];
    MetadataPickerViewModel = _metadataPickerFactory.Create(existingTemplate.Metadata);

    OutputProvider = existingTemplate.HasOutputVarName;
    OutputVariableName = existingTemplate.HasOutputVarName ? existingTemplate.OutputVarName : null;

    // Revisit this once we have some sort of caching on the UI end.
    // that way we don't have to bother the service every time
    if(existingTemplate.Metadata?.DeviceId is not null)
    {

      var deviceInfo = await _devicesClient.GetDeviceInfo(new DeviceInfoRequest { DeviceId = existingTemplate.Metadata.DeviceId }, null);
      TemplateDeviceName = string.IsNullOrEmpty(deviceInfo.Name) ? null : deviceInfo.Name;
    }
  }

  public async Task MetadataUpdated(CommandMetadata? metadata)
  {
    CommandTemplate.Metadata = metadata;
    CommandMetadata = metadata;
  }

  private async Task InitMetadata(CommandMetadata? existingMetadata)
  {
    var newArgumentDesigners = existingMetadata?.ParameterMetadatas
        ?.Select(_commandParameterDesignerFactory.Create)
        .ToArray() ?? [];

    ArgumentDesigners = newArgumentDesigners;
    if(existingMetadata?.OutputMetadata?.DataSchema is null)
    {
      OutputProvider = false;
      OutputVariableName = null;
    }

    var deviceId = existingMetadata?.DeviceId;

    if(deviceId is not null)
    {
      var deviceInfo = await _devicesClient.GetDeviceInfo(new DeviceInfoRequest { DeviceId = deviceId }, null);
      MetadataDeviceName = string.IsNullOrEmpty(deviceInfo.Name) ? null : deviceInfo.Name;
    }
  }
}
