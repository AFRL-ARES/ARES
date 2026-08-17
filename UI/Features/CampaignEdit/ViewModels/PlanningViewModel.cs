using ReactiveUI;
using System.Collections.ObjectModel;
using Ares.Datamodel.Templates;
using Ares.Datamodel.Planning;
using Ares.Core.Grpc.Services;
using UI.Application.Notifications;
using Ares.Core.Analyzing;
using Ares.Datamodel;

namespace UI.Features.CampaignEdit.ViewModels;

public class PlanningViewModel : ReactiveObject
{
  private readonly CampaignTemplate _template;
  private readonly PlannerService _client;
  private readonly IUiNotificationService _notificationService;
  private readonly IAnalyzer? _selectedAnalyzer;

  public PlanningViewModel(CampaignTemplate template, 
    IEnumerable<PlannerServiceInfo> plannerAdapters, 
    PlannerService client,
    IUiNotificationService notificationService,
    IAnalyzer? selectedAnalyzer)
  {
    _template = template;
    _client = client;
    _notificationService = notificationService;
    _selectedAnalyzer = selectedAnalyzer;
    PlannerAdapters = new ReadOnlyCollection<PlannerServiceInfo>(plannerAdapters.ToList());
    PlannerAllocationEditors = template.PlannableParameters.Select(metadata => new PlannerAllocationEditorViewModel(metadata, template.PlannerAllocations.FirstOrDefault(allocation => allocation.Parameter.Equals(metadata))?.Planner, PlannerAdapters, client, notificationService)).ToArray();
  }

  public async Task UpdateAnalyzerObjectives()
  {
    if(_selectedAnalyzer is not null)
      AvailableObjectives = await _selectedAnalyzer.GetObjectiveOutputs();

    SelectedObjectives = AvailableObjectives?.Fields.Where(obj => _template.ExperimentTemplate.PlanObjectives.Contains(obj.Key)).ToList() ?? [];
  }

  public IEnumerable<PlannerAllocationEditorViewModel> PlannerAllocationEditors { get; private set; } = [];

  public IEnumerable<PlannerServiceInfo> PlannerAdapters { get; } = [];

  public AresStructSchema? AvailableObjectives { get; private set; }

  public List<KeyValuePair<string, AresValueSchema>> SelectedObjectives { get; private set; } = [];

  public List<string> IncludedObjectives { get; private set; } = [];

  public void Save()
  {
    //We're not updating everything here maybe? Or maybe I'll just update the ARES core stuff.
    _template.PlannerAllocations.Clear();

    _template.PlannerAllocations.AddRange(PlannerAllocationEditors
      .Select(editor => editor.Save())
      .Where(allocation => allocation is not null)
      .Where(allocation => _template.PlannableParameters.Any(meta => meta.UniqueId == allocation!.Parameter.UniqueId)));

    _template.ExperimentTemplate.PlanObjectives.AddRange(SelectedObjectives.Select(obj => obj.Key));

    PlannerAllocationEditors = _template.PlannableParameters
    .Select(metadata => new PlannerAllocationEditorViewModel(metadata, _template.PlannerAllocations
    .FirstOrDefault(allocation => allocation.Parameter.Equals(metadata))?.Planner, PlannerAdapters, _client, _notificationService))
    .ToArray();
  }
}


