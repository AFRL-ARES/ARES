using Ares.Messaging;
using DynamicData;
using ReactiveUI;
using UI.Backend.ViewModels.Factories;

namespace UI.Backend.ViewModels.Automation.CampaignEdit;

public class CommandDesignerViewModel : ReactiveObject
{
  private readonly CommandParameterDesignerFactory _commandParameterDesignerFactory;
  private readonly MetadataPickerFactory _metadataPickerFactory;
  private CommandMetadata? _commandMetadata;
  private CommandTemplate _commandTemplate = null!;

  public CommandDesignerViewModel(CommandTemplate existingTemplate, CommandParameterDesignerFactory commandParameterDesignerFactory, MetadataPickerFactory metadataPickerFactory)
  {
    _commandParameterDesignerFactory = commandParameterDesignerFactory;
    _metadataPickerFactory = metadataPickerFactory;

    CommandTemplate = existingTemplate;
  }

  public CommandDesignerViewModel(CommandParameterDesignerFactory commandParameterDesignerFactory, MetadataPickerFactory metadataPickerFactory)
  {
    _commandParameterDesignerFactory = commandParameterDesignerFactory;
    _metadataPickerFactory = metadataPickerFactory;

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

  public string? TemplateDeviceName => CommandTemplate.Metadata?.DeviceName;
  public string? TemplateCommandName => CommandTemplate.Metadata?.Name;

  public bool TemplateExperimentOutputProvider => CommandTemplate.UserOutputKeyMap.Any();

  public bool ExperimentOutputProvider { get; set; }

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

  public UserOutputSelection[] OutputKeyMap { get; private set; } = [];

  public MetadataPickerViewModel? MetadataPickerViewModel { get; set; }

  public IEnumerable<CommandParameterDesignerViewModel> ArgumentDesigners { get; private set; } = [];

  public CommandTemplate Save()
  {
    CommandTemplate.Parameters.Clear();
    CommandTemplate.Parameters.AddRange(ArgumentDesigners.Select(model => model.Save()));
    if(CommandMetadata is not null)
      CommandTemplate.Metadata = CommandMetadata;

    CommandTemplate.Index = Index;
    CommandTemplate.UserOutputKeyMap.Clear();
    if(ExperimentOutputProvider)
    {
      foreach(var selection in OutputKeyMap)
      {
        CommandTemplate.UserOutputKeyMap[selection.DeviceOutputName] = selection.CustomName;
      }
    }
    else
    {
      CommandTemplate.UserOutputKeyMap.Clear();
    }

    return CommandTemplate;
  }

  private void InitTemplate(CommandTemplate existingTemplate)
  {
    Index = Convert.ToInt32(existingTemplate.Index);
    var existingParamDesigners = existingTemplate.Parameters.Select(_commandParameterDesignerFactory.Create).ToArray();
    ArgumentDesigners = [.. existingParamDesigners];
    MetadataPickerViewModel = _metadataPickerFactory.Create(existingTemplate.Metadata);

    foreach(var kvp in existingTemplate.UserOutputKeyMap)
    {
      var existingValue = OutputKeyMap.FirstOrDefault(keyValue => keyValue.DeviceOutputName == kvp.Key);
      if(existingValue is null)
      {
        continue;
      }

      existingValue.CustomName = kvp.Value;
    }

    ExperimentOutputProvider = existingTemplate.UserOutputKeyMap.Any();
  }

  private void InitMetadata(CommandMetadata? existingMetadata)
  {
    ArgumentDesigners = existingMetadata?.ParameterMetadatas.Select(_commandParameterDesignerFactory.Create).ToArray() ?? [];

    var outputs = existingMetadata?.OutputMetadata?.DataSchema;
    if(outputs is null)
    {
      OutputKeyMap = [];
      return;
    }

    var newOutputs = outputs.Fields.Where(kvp => !OutputKeyMap.Any(uos => uos.DeviceOutputName == kvp.Key)).Select(newKvp => new UserOutputSelection(newKvp.Key, newKvp.Value, newKvp.Key));
    var removedOutputs = OutputKeyMap.Where(output => !outputs.Fields.ContainsKey(output.DeviceOutputName));
    OutputKeyMap = [.. OutputKeyMap.Concat(newOutputs).Except(removedOutputs)];
  }
}

public record UserOutputSelection
{
  public UserOutputSelection(string deviceOutputName, AresDataType deviceOutputType, string customName)
  {
    DeviceOutputName = deviceOutputName;
    DeviceOutputType = deviceOutputType;
    CustomName = customName;
  }

  public string DeviceOutputName { get; }

  public AresDataType DeviceOutputType { get; }

  public string CustomName { get; set; }
}
