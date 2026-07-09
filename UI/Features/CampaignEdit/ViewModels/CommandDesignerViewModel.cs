using Ares.Core.Grpc.Services;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Templates;
using Ares.Services.Device;
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
  private CommandOutputVariableReference[] _availableVariableReferences = [];

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
      UniqueId = Guid.NewGuid().ToString(),
      DeviceCommand = new DeviceCommand()
    };
  }

  public CommandTemplate CommandTemplate
  {
    get => _commandTemplate;

    set
    {
      _commandTemplate = value;
      CommandMetadata = value.DeviceCommand?.Metadata;
      InitTemplate(value);
    }
  }

  public int Index { get; set; }

  public string? TemplateDeviceName { get; private set; }
  public string? MetadataDeviceName { get; private set; }
  public string? TemplateCommandName => CommandTemplate.DeviceCommand?.Metadata?.Name;

  public bool TemplateOutputProvider => CommandTemplate.HasOutputVarName;

  public bool OutputProvider { get; set; }

  public bool HasOutputMetadata => CommandMetadata?.OutputMetadata?.DataSchema is not null;

  public string? OutputVariableName { get; set; }

  public IEnumerable<Parameter> Arguments => CommandTemplate.ArgumentBindings;

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

  // TODO: Ensure we handle the other command template types AB 7/9/2026
  public CommandTemplate Save()
  {
    CommandTemplate.ArgumentBindings.Clear();
    CommandTemplate.ArgumentBindings.AddRange(ArgumentDesigners.Select(model => model.Save()));
    if(CommandMetadata is not null)
    {
      CommandTemplate.DeviceCommand ??= new DeviceCommand();
      CommandTemplate.DeviceCommand.Metadata = CommandMetadata;
      CommandTemplate.DeviceCommand.Metadata.DeviceType = MetadataDeviceName;
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
    var existingParamDesigners = existingTemplate.ArgumentBindings.Select(_commandParameterDesignerFactory.Create).ToArray();
    ArgumentDesigners = [.. existingParamDesigners];
    ApplyAvailableVariableReferences();
    MetadataPickerViewModel = _metadataPickerFactory.Create(existingTemplate.DeviceCommand?.Metadata);

    OutputProvider = existingTemplate.HasOutputVarName;
    OutputVariableName = existingTemplate.HasOutputVarName ? existingTemplate.OutputVarName : null;

    // Revisit this once we have some sort of caching on the UI end.
    // that way we don't have to bother the service every time
    if(existingTemplate.DeviceCommand?.Metadata?.DeviceId is not null)
    {

      var deviceInfo = await _devicesClient.GetDeviceInfo(new DeviceInfoRequest { DeviceId = existingTemplate.DeviceCommand.Metadata.DeviceId }, null);
      TemplateDeviceName = string.IsNullOrEmpty(deviceInfo.Name) ? null : deviceInfo.Name;
    }
  }

  public async Task MetadataUpdated(CommandMetadata? metadata)
  {
    CommandTemplate.DeviceCommand ??= new DeviceCommand();
    CommandTemplate.DeviceCommand.Metadata = metadata;
    CommandMetadata = metadata;
  }

  private async Task InitMetadata(CommandMetadata? existingMetadata)
  {
    var newArgumentDesigners = existingMetadata?.ParameterMetadatas
        ?.Select(_commandParameterDesignerFactory.Create)
        .ToArray() ?? [];

    ArgumentDesigners = newArgumentDesigners;
    ApplyAvailableVariableReferences();
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

  public void SetAvailableVariableReferences(IEnumerable<CommandOutputVariableReference> references)
  {
    _availableVariableReferences = references.ToArray();
    ApplyAvailableVariableReferences();
  }

  public CommandOutputVariableReference[] GetOutputVariableReferences()
    => CommandOutputVariableReferenceBuilder.Build(this);

  public string? GetParameterAssignmentError(Parameter parameter)
  {
    var argumentDesigner = ArgumentDesigners.FirstOrDefault(designer => designer.Name == parameter.Metadata?.Name);
    if(argumentDesigner is null)
      return "Parameter is no longer available for this command.";

    return parameter.GetParameterSource() switch
    {
      ParameterSource.Planned when !argumentDesigner.HasSelectedPlannedParameter()
        => "Planned parameter is no longer available.",

      ParameterSource.Variable when !argumentDesigner.HasSelectedVariableReference()
        => "Command output variable is no longer available.",

      _ => null
    };
  }

  private void ApplyAvailableVariableReferences()
  {
    foreach(var argumentDesigner in ArgumentDesigners)
      argumentDesigner.SetAvailableVariableReferences(_availableVariableReferences);
  }
}
