using Ares.Messaging.Planning;
using ReactiveUI;
using System.Collections.ObjectModel;
using Ares.Datamodel;
using Ares.Datamodel.Templates;
using UI.Backend.Extensions;

namespace UI.Backend.ViewModels.Automation.CampaignEdit;

public class PlanningViewModel : ReactiveObject
{
  private readonly CampaignTemplate _template;
  private readonly AresPlanning.AresPlanningClient _client;

  public PlanningViewModel(CampaignTemplate template, IEnumerable<PlannerAdapterInfo> plannerAdapters, AresPlanning.AresPlanningClient client)
  {
    _template = template;
    _client = client;
    PlannerAdapters = new ReadOnlyCollection<PlannerAdapterInfo>(plannerAdapters.ToList());
    PlannerAllocationEditors = template.PlannableParameters.Select(metadata => new PlannerAllocationEditorViewModel(metadata, template.PlannerAllocations.FirstOrDefault(allocation => allocation.Parameter.Equals(metadata))?.Planner, PlannerAdapters, client)).ToArray();
  }

  public IEnumerable<PlannerAllocationEditorViewModel> PlannerAllocationEditors { get; private set; }

  public IEnumerable<PlannerAdapterInfo> PlannerAdapters { get; }

  public void Save()
  {
    //We're not updating everything here maybe? Or maybe I'll just update the ARES core stuff.
    _template.PlannerAllocations.Clear();

    _template.PlannerAllocations.AddRange(PlannerAllocationEditors
      .Select(editor => editor.Save())
      .Where(allocation => allocation is not null)
      .Where(allocation => _template.PlannableParameters.Any(meta => meta.UniqueId == allocation!.Parameter.UniqueId)));

    PlannerAllocationEditors = _template.PlannableParameters
    .Select(metadata => new PlannerAllocationEditorViewModel(metadata, _template.PlannerAllocations
    .FirstOrDefault(allocation => allocation.Parameter.Equals(metadata))?.Planner, PlannerAdapters, _client))
    .ToArray();
  }
}
