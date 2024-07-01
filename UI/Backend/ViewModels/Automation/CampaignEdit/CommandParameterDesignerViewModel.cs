using Ares.Messaging;
using ReactiveUI;
using UI.Backend.Helpers;

namespace UI.Backend.ViewModels.Automation.CampaignEdit;

public class CommandParameterDesignerViewModel : ReactiveObject
{
  private readonly IEnumerable<ParameterMetadata>? _plannedParameters;
  private readonly UnitCategoryHelper _unitCategoryHelper;
  private bool _isPlanned;
  private Parameter _parameter = null!;
  private bool _valid;
  private ParameterValue? _value;

  public CommandParameterDesignerViewModel(Parameter param, UnitCategoryHelper unitCategoryHelper, IEnumerable<ParameterMetadata>? plannedParameters = null)
    : this(unitCategoryHelper, plannedParameters)
  {
    Parameter = param;
  }

  public CommandParameterDesignerViewModel(ParameterMetadata meta, UnitCategoryHelper unitCategoryHelper, IEnumerable<ParameterMetadata>? plannedParameters = null)
    : this(unitCategoryHelper, plannedParameters)
  {
    Parameter = new Parameter
    {
      UniqueId = Guid.NewGuid().ToString(),
      Metadata = meta,
      Value = new ParameterValue
      {
        UniqueId = Guid.NewGuid().ToString()
      }
    };
  }

  private CommandParameterDesignerViewModel(UnitCategoryHelper unitCategoryHelper, IEnumerable<ParameterMetadata>? plannedParameters)
  {
    _unitCategoryHelper = unitCategoryHelper;
    _plannedParameters = plannedParameters;
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

  public ParameterValue? Value
  {
    get => _value;

    set
    {
      this.RaiseAndSetIfChanged(ref _value, value);
      if (value is null)
        return;

      Valid = IsValid(value.Value);
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

  public IEnumerable<ParameterMetadata> PlannedParameters { get; private set; } = Array.Empty<ParameterMetadata>();

  public string? SelectedPlannedParameterMetadataId { get; set; }

  public bool IsPlanned
  {
    get => _isPlanned;

    set
    {
      _isPlanned = value;
      Value = value ? null : Parameter.Value ?? new ParameterValue { UniqueId = Guid.NewGuid().ToString() };
    }
  }

  private IEnumerable<ParameterMetadata> FilterParameterMetadata(UnitCategoryHelper helper, IEnumerable<ParameterMetadata>? allMetadata)
  {
    if (allMetadata is null)
      return Array.Empty<ParameterMetadata>();

    if (string.IsNullOrEmpty(Unit))
      return allMetadata;

    return allMetadata.Where(metadata => helper.GetCategoryForUnit(metadata.Unit) == helper.GetCategoryForUnit(Unit));
  }

  private void Init(Parameter existingParameter)
  {
    Value = existingParameter.Value;
    IsPlanned = existingParameter.Planned;
    SelectedPlannedParameterMetadataId = existingParameter.PlanningMetadata?.UniqueId;
    PlannedParameters = FilterParameterMetadata(_unitCategoryHelper, _plannedParameters);
  }

  public Parameter Save()
  {
    Parameter.Value = Value;
    Parameter.Planned = IsPlanned;
    Parameter.PlanningMetadata = Parameter.Planned ? PlannedParameters.FirstOrDefault(metadata => metadata.UniqueId == SelectedPlannedParameterMetadataId) : null;

    return Parameter;
  }

  private bool IsValid(float val)
  {
    if (Parameter.Metadata.Constraints.Count == 0)
      return true;

    return Parameter.Metadata.Constraints.Any(limits => val >= limits.Minimum && val <= limits.Maximum);
  }
}
