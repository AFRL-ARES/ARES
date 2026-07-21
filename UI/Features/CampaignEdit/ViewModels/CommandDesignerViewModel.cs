using Ares.Core.CustomCommands;
using Ares.Core.Execution.Executors;
using Ares.Core.Grpc.Services;
using Ares.Datamodel;
using Ares.Datamodel.Automation;
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
  private readonly ICustomCommandPersistenceService _customCommandPersistenceService;
  private readonly Task _initializationTask;
  private CommandMetadata? _commandMetadata;
  private CustomCommandVersion? _selectedCustomCommand;
  private CommandOutputVariableReference[] _availableVariableReferences = [];

  public CommandDesignerViewModel(
    CommandTemplate existingTemplate,
    CommandParameterDesignerFactory commandParameterDesignerFactory,
    MetadataPickerFactory metadataPickerFactory,
    DevicesService devicesClient,
    ICustomCommandPersistenceService customCommandPersistenceService)
  {
    ArgumentDesigners = [];
    AvailableCustomCommands = [];
    _commandParameterDesignerFactory = commandParameterDesignerFactory;
    _metadataPickerFactory = metadataPickerFactory;
    _devicesClient = devicesClient;
    _customCommandPersistenceService = customCommandPersistenceService;
    CommandTemplate = existingTemplate;
    _initializationTask = InitializeAsync(existingTemplate);
  }

  public CommandDesignerViewModel(
    CommandParameterDesignerFactory commandParameterDesignerFactory,
    MetadataPickerFactory metadataPickerFactory,
    DevicesService devicesClient,
    ICustomCommandPersistenceService customCommandPersistenceService)
    : this(
      new CommandTemplate
      {
        UniqueId = Guid.NewGuid().ToString(),
        DeviceCommand = new DeviceCommand()
      },
      commandParameterDesignerFactory,
      metadataPickerFactory,
      devicesClient,
      customCommandPersistenceService)
  {
  }

  public CommandTemplate CommandTemplate { get; }

  public int Index { get; set; }

  private string? TemplateDeviceName { get; set; }

  private string? MetadataDeviceName { get; set; }

  public string? TemplateCommandName => CommandTypeCase switch
  {
    CommandTemplate.CommandTypeOneofCase.DeviceCommand => CommandMetadata?.Name,
    CommandTemplate.CommandTypeOneofCase.SystemCommand => SelectedSystemOperationDefinition?.DisplayName,
    CommandTemplate.CommandTypeOneofCase.CustomCommandInvocation => _selectedCustomCommand?.Name
      ?? (string.IsNullOrWhiteSpace(SelectedCustomCommandId) ? null : "Unknown Custom Command"),
    CommandTemplate.CommandTypeOneofCase.None => null,
    _ => throw new ArgumentOutOfRangeException(nameof(CommandTypeCase), CommandTypeCase, null)
  };

  public string? TemplateCommandDescription => CommandTypeCase switch
  {
    CommandTemplate.CommandTypeOneofCase.DeviceCommand => CommandMetadata?.Description,
    CommandTemplate.CommandTypeOneofCase.SystemCommand => SelectedSystemOperationDefinition?.Description,
    CommandTemplate.CommandTypeOneofCase.CustomCommandInvocation => _selectedCustomCommand?.Description,
    CommandTemplate.CommandTypeOneofCase.None => null,
    _ => throw new ArgumentOutOfRangeException(nameof(CommandTypeCase), CommandTypeCase, null)
  };

  public string? CommandTargetName => CommandTypeCase switch
  {
    CommandTemplate.CommandTypeOneofCase.DeviceCommand => MetadataDeviceName ?? TemplateDeviceName,
    CommandTemplate.CommandTypeOneofCase.SystemCommand => "SYSTEM",
    CommandTemplate.CommandTypeOneofCase.CustomCommandInvocation => "CUSTOM",
    CommandTemplate.CommandTypeOneofCase.None => null,
    _ => throw new ArgumentOutOfRangeException(nameof(CommandTypeCase), CommandTypeCase, null)
  };

  public bool IsCommandUnavailable => CommandTypeCase switch
  {
    CommandTemplate.CommandTypeOneofCase.DeviceCommand => CommandMetadata is null,
    CommandTemplate.CommandTypeOneofCase.SystemCommand => SelectedSystemOperationDefinition is null,
    CommandTemplate.CommandTypeOneofCase.CustomCommandInvocation => !string.IsNullOrWhiteSpace(SelectedCustomCommandId)
      && _selectedCustomCommand is null,
    CommandTemplate.CommandTypeOneofCase.None => true,
    _ => throw new ArgumentOutOfRangeException(nameof(CommandTypeCase), CommandTypeCase, null)
  };

  public CommandTemplate.CommandTypeOneofCase CommandTypeCase => CommandTemplate.CommandTypeCase;

  public int SelectedCommandTabIndex
  {
    get => CommandTypeCase switch
    {
      CommandTemplate.CommandTypeOneofCase.DeviceCommand => 0,
      CommandTemplate.CommandTypeOneofCase.SystemCommand => 1,
      CommandTemplate.CommandTypeOneofCase.CustomCommandInvocation => 2,
      CommandTemplate.CommandTypeOneofCase.None => 0,
      _ => throw new ArgumentOutOfRangeException(nameof(CommandTypeCase), CommandTypeCase, null)
    };
    set => SelectCommandType(value);
  }

  public static IReadOnlyList<SystemOperationDefinition> AvailableSystemOperations => SystemOperationCatalog.Definitions;

  public SystemOperation SelectedSystemOperation => CommandTemplate.SystemCommand?.Operation ?? SystemOperation.Undefined;

  private SystemOperationDefinition? SelectedSystemOperationDefinition
    => SystemOperationCatalog.Find(SelectedSystemOperation);

  public IReadOnlyList<CustomCommandVersion> AvailableCustomCommands { get; private set; }

  public string? CustomCommandLoadError { get; private set; }

  public string? SelectedCustomCommandId => CommandTemplate.CustomCommandInvocation?.CustomCommandId;

  public bool TemplateOutputProvider => CommandTemplate.HasOutputVarName;

  public bool OutputProvider { get; set; }

  public AresValueSchema? OutputSchema => CommandTypeCase switch
  {
    CommandTemplate.CommandTypeOneofCase.DeviceCommand => CommandMetadata?.OutputMetadata?.DataSchema,
    CommandTemplate.CommandTypeOneofCase.SystemCommand => SelectedSystemOperationDefinition?.OutputSchema,
    CommandTemplate.CommandTypeOneofCase.CustomCommandInvocation => _selectedCustomCommand?.OutputSchema,
    CommandTemplate.CommandTypeOneofCase.None => null,
    _ => throw new ArgumentOutOfRangeException(nameof(CommandTypeCase), CommandTypeCase, null)
  };

  public bool HasOutputMetadata => OutputSchema is not null
    && OutputSchema.Type is not AresDataType.Unit and not AresDataType.UnspecifiedType;

  public string? OutputVariableName { get; set; }

  public IEnumerable<Parameter> Arguments => CommandTemplate.ArgumentBindings;

  public CommandMetadata? CommandMetadata
  {
    get => _commandMetadata;
    private set => this.RaiseAndSetIfChanged(ref _commandMetadata, value);
  }

  public MetadataPickerViewModel? MetadataPickerViewModel { get; private set; }

  [Reactive]
  public partial IEnumerable<CommandParameterDesignerViewModel> ArgumentDesigners { get; private set; }

  public Task EnsureInitializedAsync() => _initializationTask;

  public CommandTemplate Save()
  {
    CommandTemplate.ArgumentBindings.Clear();
    CommandTemplate.ArgumentBindings.AddRange(ArgumentDesigners.Select(model => model.Save()));

    if(CommandTypeCase == CommandTemplate.CommandTypeOneofCase.DeviceCommand && CommandMetadata is not null)
    {
      CommandTemplate.DeviceCommand.Metadata = CommandMetadata;
      if(!string.IsNullOrWhiteSpace(MetadataDeviceName))
        CommandTemplate.DeviceCommand.Metadata.DeviceType = MetadataDeviceName;
    }

    CommandTemplate.Index = Index;
    CommandTemplate.ClearOutputVarName();
    if(OutputProvider && HasOutputMetadata && !string.IsNullOrWhiteSpace(OutputVariableName))
      CommandTemplate.OutputVarName = OutputVariableName.Trim();

    return CommandTemplate;
  }

  public async Task DeviceCommandMetadataUpdated(CommandMetadata? metadata)
  {
    CommandTemplate.DeviceCommand ??= new DeviceCommand();
    CommandTemplate.DeviceCommand.Metadata = metadata;
    CommandMetadata = metadata;
    await ApplyDeviceMetadataAsync(metadata, preserveBindings: false);
    RaiseCommandPropertiesChanged();
  }

  public void SelectSystemOperation(SystemOperation operation)
  {
    CommandTemplate.SystemCommand ??= new SystemCommand();
    CommandTemplate.SystemCommand.Operation = operation;
    var definition = SystemOperationCatalog.Find(operation);
    SetArgumentDefinitions(definition?.Parameters ?? []);
    ResetOutputAssignment();
    RaiseCommandPropertiesChanged();
  }

  public void SelectCustomCommand(string? customCommandId)
  {
    CommandTemplate.CustomCommandInvocation ??= new CustomCommandInvocation();
    CommandTemplate.CustomCommandInvocation.CustomCommandId = customCommandId ?? string.Empty;
    _selectedCustomCommand = AvailableCustomCommands.FirstOrDefault(command => command.CustomCommandId == customCommandId);
    SetArgumentDefinitions(BuildCustomParameterMetadata(_selectedCustomCommand));
    ResetOutputAssignment();
    RaiseCommandPropertiesChanged();
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

      ParameterSource.Planned or ParameterSource.Variable
        => null,

      ParameterSource.Unspecified or ParameterSource.Value or ParameterSource.Environment
        => null,

      _ => throw new ArgumentOutOfRangeException(nameof(parameter), parameter.GetParameterSource(), null)
    };
  }

  private async Task InitializeAsync(CommandTemplate existingTemplate)
  {
    Index = Convert.ToInt32(existingTemplate.Index);
    OutputProvider = existingTemplate.HasOutputVarName;
    OutputVariableName = existingTemplate.HasOutputVarName ? existingTemplate.OutputVarName : null;
    try
    {
      AvailableCustomCommands = (await _customCommandPersistenceService.GetCommandsAsync())
        .OrderBy(command => command.Name)
        .ToArray();
    }
    catch(Exception exception)
    {
      AvailableCustomCommands = [];
      CustomCommandLoadError = $"Failed to load custom commands. {exception.Message}";
    }

    switch(existingTemplate.CommandTypeCase)
    {
      case CommandTemplate.CommandTypeOneofCase.SystemCommand:
        SetArgumentDefinitions(SelectedSystemOperationDefinition?.Parameters ?? [], existingTemplate.ArgumentBindings);
        break;

      case CommandTemplate.CommandTypeOneofCase.CustomCommandInvocation:
        _selectedCustomCommand = AvailableCustomCommands.FirstOrDefault(command => command.CustomCommandId == SelectedCustomCommandId);
        var customDefinitions = BuildCustomParameterMetadata(_selectedCustomCommand);
        if(_selectedCustomCommand is null)
          // The saved custom command may have been deleted or failed to load. Preserve its
          // saved arguments so the campaign can still be displayed and edited.
          SetExistingArguments(existingTemplate.ArgumentBindings);
        else
          SetArgumentDefinitions(customDefinitions, existingTemplate.ArgumentBindings);
        break;

      case CommandTemplate.CommandTypeOneofCase.DeviceCommand:
        CommandMetadata = existingTemplate.DeviceCommand?.Metadata;
        MetadataPickerViewModel = _metadataPickerFactory.Create(CommandMetadata);
        SetArgumentDefinitions(CommandMetadata?.ParameterMetadatas ?? [], existingTemplate.ArgumentBindings);
        await LoadDeviceNamesAsync(CommandMetadata);
        break;

      case CommandTemplate.CommandTypeOneofCase.None:
        throw new ArgumentOutOfRangeException(nameof(existingTemplate.CommandTypeCase), existingTemplate.CommandTypeCase, null);

      default:
        throw new ArgumentOutOfRangeException(nameof(existingTemplate.CommandTypeCase), existingTemplate.CommandTypeCase, null);
    }

    if(!HasOutputMetadata)
      ResetOutputAssignment();

    RaiseCommandPropertiesChanged();
  }

  private void SelectCommandType(int tabIndex)
  {
    var commandType = tabIndex switch
    {
      0 => CommandTemplate.CommandTypeOneofCase.DeviceCommand,
      1 => CommandTemplate.CommandTypeOneofCase.SystemCommand,
      2 => CommandTemplate.CommandTypeOneofCase.CustomCommandInvocation,
      _ => throw new ArgumentOutOfRangeException(nameof(tabIndex), tabIndex, null)
    };

    if(commandType == CommandTypeCase)
      return;

    CommandTemplate.ClearCommandType();
    CommandMetadata = null;
    _selectedCustomCommand = null;
    TemplateDeviceName = null;
    MetadataDeviceName = null;
    ArgumentDesigners = [];
    ResetOutputAssignment();

    switch(commandType)
    {
      case CommandTemplate.CommandTypeOneofCase.SystemCommand:
        CommandTemplate.SystemCommand = new SystemCommand();
        break;

      case CommandTemplate.CommandTypeOneofCase.CustomCommandInvocation:
        CommandTemplate.CustomCommandInvocation = new CustomCommandInvocation();
        break;

      case CommandTemplate.CommandTypeOneofCase.DeviceCommand:
        CommandTemplate.DeviceCommand = new DeviceCommand();
        MetadataPickerViewModel = _metadataPickerFactory.Create();
        break;

      case CommandTemplate.CommandTypeOneofCase.None:
      default:
        throw new ArgumentOutOfRangeException(nameof(commandType), commandType, null);
    }

    RaiseCommandPropertiesChanged();
  }

  private async Task ApplyDeviceMetadataAsync(CommandMetadata? metadata, bool preserveBindings)
  {
    var existingBindings = preserveBindings ? CommandTemplate.ArgumentBindings : [];
    SetArgumentDefinitions(metadata?.ParameterMetadatas ?? [], existingBindings);
    ResetOutputAssignment();
    await LoadDeviceNamesAsync(metadata);
  }

  private async Task LoadDeviceNamesAsync(CommandMetadata? metadata)
  {
    MetadataDeviceName = null;
    if(string.IsNullOrWhiteSpace(metadata?.DeviceId))
      return;

    try
    {
      var deviceInfo = await _devicesClient.GetDeviceInfo(new DeviceInfoRequest { DeviceId = metadata.DeviceId }, null);
      var deviceName = string.IsNullOrWhiteSpace(deviceInfo.Name) ? null : deviceInfo.Name;
      MetadataDeviceName = deviceName;
      TemplateDeviceName ??= deviceName;
    }
    catch
    {
      MetadataDeviceName = null;
    }
  }

  private void SetArgumentDefinitions(
    IEnumerable<ParameterMetadata> definitions,
    IEnumerable<Parameter>? existingBindings = null)
  {
    var bindingsByName = (existingBindings ?? [])
      .Where(binding => binding.Metadata is not null)
      .GroupBy(binding => binding.Metadata.Name, StringComparer.Ordinal)
      .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

    ArgumentDesigners = definitions.Select(definition =>
    {
      if(!bindingsByName.TryGetValue(definition.Name, out var binding))
        return _commandParameterDesignerFactory.Create(CloneBindingMetadata(definition));

      var normalizedBinding = binding.Clone();
      normalizedBinding.Metadata = CloneBindingMetadata(definition, binding.Metadata?.UniqueId);
      return _commandParameterDesignerFactory.Create(normalizedBinding);
    }).ToArray();
    ApplyAvailableVariableReferences();
  }

  private static ParameterMetadata CloneBindingMetadata(ParameterMetadata definition, string? existingBindingMetadataId = null)
  {
    var metadata = definition.Clone();
    metadata.UniqueId = !string.IsNullOrWhiteSpace(existingBindingMetadataId)
      && !string.Equals(existingBindingMetadataId, definition.UniqueId, StringComparison.OrdinalIgnoreCase)
        ? existingBindingMetadataId
        : Guid.NewGuid().ToString();
    return metadata;
  }

  private void SetExistingArguments(IEnumerable<Parameter> bindings)
  {
    ArgumentDesigners = bindings
      .Select(binding => _commandParameterDesignerFactory.Create(binding.Clone()))
      .ToArray();
    ApplyAvailableVariableReferences();
  }

  private static ParameterMetadata[] BuildCustomParameterMetadata(CustomCommandVersion? command)
    => command?.InputParameters
      .Select((parameter, index) => new ParameterMetadata
      {
        UniqueId = Guid.NewGuid().ToString(),
        Name = parameter.Name,
        Index = index,
        Schema = parameter.Schema?.Clone() ?? new AresValueSchema()
      })
      .ToArray() ?? [];

  private void ResetOutputAssignment()
  {
    OutputProvider = false;
    OutputVariableName = null;
    CommandTemplate.ClearOutputVarName();
  }

  private void ApplyAvailableVariableReferences()
  {
    foreach(var argumentDesigner in ArgumentDesigners)
      argumentDesigner.SetAvailableVariableReferences(_availableVariableReferences);
  }

  private void RaiseCommandPropertiesChanged()
  {
    this.RaisePropertyChanged(nameof(CommandTypeCase));
    this.RaisePropertyChanged(nameof(SelectedCommandTabIndex));
    this.RaisePropertyChanged(nameof(SelectedSystemOperation));
    this.RaisePropertyChanged(nameof(SelectedCustomCommandId));
    this.RaisePropertyChanged(nameof(CustomCommandLoadError));
    this.RaisePropertyChanged(nameof(TemplateCommandName));
    this.RaisePropertyChanged(nameof(TemplateCommandDescription));
    this.RaisePropertyChanged(nameof(CommandTargetName));
    this.RaisePropertyChanged(nameof(IsCommandUnavailable));
    this.RaisePropertyChanged(nameof(OutputSchema));
    this.RaisePropertyChanged(nameof(HasOutputMetadata));
  }
}
