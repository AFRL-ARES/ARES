using Ares.Messaging;
using Ares.Messaging.Planning;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace UI.Backend.ViewModels.Automation.CampaignEdit;

public class PlannerAllocationEditorViewModel : ReactiveObject
{
  private readonly AresPlanning.AresPlanningClient _plannerClient;
  private string _selectedPlannerOption = string.Empty;
  private PlannerAdapterInfo? _selectedAdapter;

  public PlannerAllocationEditorViewModel(ParameterMetadata metadata,
    PlannerAdapterInfo? plannerInfo,
    IEnumerable<PlannerAdapterInfo> plannerAdapters,
    AresPlanning.AresPlanningClient plannerClient)
  {
    var plannerArray = plannerAdapters.ToArray();

    ParameterMetadata = metadata;
    PlannerServices = plannerArray;
    _plannerClient = plannerClient;

    if(plannerInfo is not null)
    {
      SelectedService = plannerInfo;
      SelectedPlannerOption = metadata.PlannerName;
    }
  }

  public PlannerAllocation? Save()
  {
    if(SelectedService is null)
      return null;

    var allocation = new PlannerAllocation
    {
      Parameter = ParameterMetadata,
      Planner = SelectedService,
      UniqueId = Guid.NewGuid().ToString()
    };

    var selectedPlannerOption = PlannerOptions.First(option => option.Name == _selectedPlannerOption);
    allocation.Parameter.PlannerName = _selectedPlannerOption;
    allocation.Parameter.PlannerDescription = selectedPlannerOption.Description;

    return allocation;
  }

  public async Task UpdatePlannerOptions()
  {
    if(SelectedService is null)
      return;

    var response = await _plannerClient.GetPlannerCapabilitiesAsync(new CapabilitiesRequest { AdapterName = SelectedService.AdapterName });
    PlannerOptions = response.PlannerCapability.AsEnumerable();

    //Not all adapters will have multiple options, if not auto assign the value
    if(PlannerOptions.Count() <= 1)
      SelectedPlannerOption = SelectedService.AdapterName;
  }

  public async Task UpdatePlannerSettings()
  {
    if(SelectedService is null)
      return;

    var response = await _plannerClient.GetPlannerSettingsAsync(new PlannerSettingsRequest { ServiceName = SelectedService.AdapterName, PlannerName = SelectedPlannerOption });
    PlannerSettings = response.Settings;
    PlannerDescription = PlannerOptions.First(p => p.Name == SelectedPlannerOption).Description;
  }

  public PlannerAdapterInfo? SelectedService
  {
    get => _selectedAdapter;

    set
    {
      if(value is null || _selectedAdapter == value)
        return;

      if(_selectedAdapter is not null)
        _selectedPlannerOption = string.Empty;

      _selectedAdapter = value;
      _ = UpdatePlannerOptions();
    }
  }

  public string? SelectedPlannerOption
  {
    get => _selectedPlannerOption;

    set
    {
      if(value is null || _selectedPlannerOption == value)
        return;

      _selectedPlannerOption = value;
      _ = UpdatePlannerSettings();
    }
  }

  [Reactive]
  public IEnumerable<PlannerOption> PlannerOptions { get; set; } = Enumerable.Empty<PlannerOption>();
  [Reactive]
  public IEnumerable<PlannerSetting> PlannerSettings { get; set; } = Enumerable.Empty<PlannerSetting>();
  [Reactive]
  public string PlannerDescription { get; set; } = string.Empty;
  public ParameterMetadata ParameterMetadata { get; }
  public IEnumerable<PlannerAdapterInfo> PlannerServices { get; }

}
