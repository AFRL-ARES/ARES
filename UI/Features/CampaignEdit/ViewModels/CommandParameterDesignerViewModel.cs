using Ares.Datamodel;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Templates;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using UI.Components.Formatting;

namespace UI.Features.CampaignEdit.ViewModels;

public partial class CommandParameterDesignerViewModel : ReactiveObject
{
  private readonly ParameterMetadata[]? _plannedParameters;
  private readonly UnitCategoryHelper _unitCategoryHelper;
  private Parameter _parameter = null!;
  private ParameterSource _selectedParameterSource;
  private bool _valid;
  private AresValue? _value;
  private CommandOutputVariableReference[] _availableVariableReferences = [];
  private CommandOutputVariableOption[] _availableVariableOptions = [];

  public CommandParameterDesignerViewModel(Parameter param, UnitCategoryHelper unitCategoryHelper, IEnumerable<ParameterMetadata>? plannedParameters = null)
    : this(unitCategoryHelper, plannedParameters)
  {
    Parameter = param;
    SelectedVariableType = param.GetVariableType();
  }

  public CommandParameterDesignerViewModel(ParameterMetadata meta, UnitCategoryHelper unitCategoryHelper, IEnumerable<ParameterMetadata>? plannedParameters = null)
    : this(unitCategoryHelper, plannedParameters)
  {
    Parameter = new Parameter
    {
      UniqueId = Guid.NewGuid().ToString(),
      Metadata = meta
    };

    Value = new AresValue();
  }

  private CommandParameterDesignerViewModel(UnitCategoryHelper unitCategoryHelper, IEnumerable<ParameterMetadata>? plannedParameters)
  {
    _unitCategoryHelper = unitCategoryHelper;
    _plannedParameters = plannedParameters?.ToArray();
  }

  public Parameter Parameter
  {
    private get => _parameter;

    set
    {
      _parameter = value;
      Init(value);
    }
  }

  public string Name => Parameter.Metadata.Name;

  public string Unit => Parameter.Metadata.Unit;

  public AresValueSchema Schema => Parameter.Metadata.Schema;

  public AresValue? Value
  {
    get => _value;

    set
    {
      this.RaiseAndSetIfChanged(ref _value, value);
      if(value is null)
        return;

      Valid = IsValid(value);
    }
  }

  public bool Valid
  {
    get => _valid;

    private set
    {
      _valid = value;
      this.RaisePropertyChanged();
    }
  }

  public ParameterMetadata[] PlannedParameters { get; private set; } = [];

  public string? SelectedPlannedParameterMetadataId { get; set; }

  public VariableType? SelectedVariableType { get; set; }

  [Reactive]
  public partial string? SelectedVariableArgument { get; set; }

  public VariableType[] VariableTypes { get; private set; } = System.Enum.GetValues<VariableType>().Skip(1).ToArray();

  public ParameterSource SelectedParameterSource
  {
    get => _selectedParameterSource;

    set
    {
      this.RaiseAndSetIfChanged(ref _selectedParameterSource, value);
      Value = value == ParameterSource.Value ? Parameter.GetValue() ?? new AresValue() : null;
    }
  }

  public bool IsPlanned => SelectedParameterSource == ParameterSource.Planned;

  public bool IsEnvironmentBased => SelectedParameterSource == ParameterSource.Environment;

  public bool IsValueBased => SelectedParameterSource == ParameterSource.Value;

  public bool IsVariableBased => SelectedParameterSource == ParameterSource.Variable;

  public ParameterSource[] ParameterSources { get; } = Enum.GetValues<ParameterSource>().Skip(1).ToArray();

  public CommandOutputVariableReference[] AvailableVariableReferences
  {
    get => _availableVariableReferences;
    private set
    {
      this.RaiseAndSetIfChanged(ref _availableVariableReferences, value);
      AvailableVariableOptions = value
        .Select(reference => new CommandOutputVariableOption(reference.Path, reference.DisplayText, reference.IsDisabled))
        .ToArray();
    }
  }

  public CommandOutputVariableOption[] AvailableVariableOptions
  {
    get => _availableVariableOptions;
    private set => this.RaiseAndSetIfChanged(ref _availableVariableOptions, value);
  }

  public int PastExperimentNumber { get; set; }

  private ParameterMetadata[] FilterParameterMetadata(UnitCategoryHelper helper, IEnumerable<ParameterMetadata>? allMetadata)
  {
    if(allMetadata is null)
      return [];

    return allMetadata.ToArray();
  }

  private void Init(Parameter existingParameter)
  {
    Value = existingParameter.GetValue();
    SelectedParameterSource = DetermineParameterSource(existingParameter);
    SelectedPlannedParameterMetadataId = existingParameter.GetPlanningMetadata()?.UniqueId;
    SelectedVariableArgument = DetermineParameterSource(existingParameter) == ParameterSource.Variable ? existingParameter.GetVariableArgument() : null;
    SelectedVariableType = existingParameter.GetVariableType() == VariableType.VarUnspecified ? null : existingParameter.GetVariableType();
    PlannedParameters = FilterParameterMetadata(_unitCategoryHelper, _plannedParameters);
    PastExperimentNumber = DeterminePastExperimentNumber(existingParameter.GetVariableArgument());
  }

  public void SetAvailableVariableReferences(IEnumerable<CommandOutputVariableReference> references)
  {
    AvailableVariableReferences = CommandOutputVariableReferenceBuilder.MarkCompatibility(references, Schema);
  }

  private ParameterSource DetermineParameterSource(Parameter parameter)
  {
    return parameter.GetParameterSource() == ParameterSource.Unspecified
      ? ParameterSource.Value
      : parameter.GetParameterSource();
  }

  private int DeterminePastExperimentNumber(string arg)
  {
    var parsed = int.TryParse(arg, out var intValue);

    if(parsed)
      return intValue;

    return 0;
  }

  public Parameter Save()
  {
    switch(SelectedParameterSource)
    {
      case ParameterSource.Planned:
        Parameter.SetPlannedSource(PlannedParameters.FirstOrDefault(metadata => metadata.UniqueId == SelectedPlannedParameterMetadataId));
        break;

      case ParameterSource.Environment:
        var variableType = SelectedVariableType ?? VariableType.VarUnspecified;
        var variableArgument = variableType == VariableType.PreviousExperimentPath ? PastExperimentNumber.ToString() : "";
        Parameter.SetEnvironmentSource(variableType, variableArgument);
        break;

      case ParameterSource.Variable:
        Parameter.SetCommandVariableSource(SelectedVariableArgument ?? "");
        break;

      default:
        Parameter.SetLiteralSource(Value);
        break;
    }

    return Parameter;
  }

  private bool IsValid(AresValue value)
  {
    switch(Schema.Type)
    {
      case AresDataType.Number:
        if(!value.HasNumberValue)
          return false;

        if(Parameter.Metadata.Constraints.Count == 0)
          return true;

        return Parameter.Metadata.Constraints.Any(limits => value.NumberValue >= limits.Minimum && value.NumberValue <= limits.Maximum);

      case AresDataType.String:
        return value.HasStringValue;

      default:
        return true;
    }
  }
}

public record CommandOutputVariableOption(string Value, string Text, bool Disabled);
