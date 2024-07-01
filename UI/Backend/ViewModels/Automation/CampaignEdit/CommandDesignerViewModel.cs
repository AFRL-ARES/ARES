using Ares.Messaging;
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
      Init(value);
    }
  }

  public int Index { get; set; }

  public string? TemplateDeviceName => CommandTemplate.Metadata?.DeviceName;
  public string? TemplateCommandName => CommandTemplate.Metadata?.Name;

  public bool ExperimentOutputProvider { get; set; }

  public IEnumerable<Parameter> Arguments => CommandTemplate.Parameters;

  public CommandMetadata? CommandMetadata
  {
    get => _commandMetadata;

    set
    {
      _commandMetadata = value;
      InitNewMetadata(value);
    }
  }

  public MetadataPickerViewModel? MetadataPickerViewModel { get; set; }

  public IEnumerable<CommandParameterDesignerViewModel> ArgumentDesigners { get; set; } = Array.Empty<CommandParameterDesignerViewModel>();

  public CommandTemplate Save()
  {
    CommandTemplate.Parameters.Clear();
    CommandTemplate.Parameters.AddRange(ArgumentDesigners.Select(model => model.Save()));
    if (CommandMetadata is not null)
      CommandTemplate.Metadata = CommandMetadata;

    CommandTemplate.Index = Index;
    return CommandTemplate;
  }

  private void Init(CommandTemplate existingTemplate)
  {
    Index = Convert.ToInt32(existingTemplate.Index);
    var existingParamDesigners = existingTemplate.Parameters.Select(parameter => _commandParameterDesignerFactory.Create(parameter)).ToArray();
    ArgumentDesigners = existingParamDesigners.ToArray();
    MetadataPickerViewModel = _metadataPickerFactory.Create(existingTemplate.Metadata);
  }

  private void InitNewMetadata(CommandMetadata? existingMetadata)
  {
    ArgumentDesigners = existingMetadata?.ParameterMetadatas.Select(metadata => _commandParameterDesignerFactory.Create(metadata)).ToArray() ?? Array.Empty<CommandParameterDesignerViewModel>();
  }
}
