using System.Collections.ObjectModel;
using Ares.Messaging;
using ReactiveUI;

namespace UI.Backend.ViewModels.Automation.CampaignEdit;

public class PlanningViewModel : ReactiveObject
{
  // private readonly IEnumerable<PlannerAllocationEditorViewModel> _plannerAllocations;
  private readonly CampaignTemplate _template;

  public PlanningViewModel(CampaignTemplate template, IEnumerable<PlannerInfo> planners)
  {
    _template = template;
    Planners = new ReadOnlyCollection<PlannerInfo>(planners.ToList());
    PlannerAllocationEditors = template.PlannableParameters.Select(metadata => new PlannerAllocationEditorViewModel(metadata, template.PlannerAllocations.FirstOrDefault(allocation => allocation.Parameter.Equals(metadata))?.Planner, Planners)).ToArray();
  }

  // public IEnumerable<ParameterMetadata> ParameterMetadatas => new ObservableCollection<ParameterMetadata>(_template.PlannableParameters);

  public IEnumerable<PlannerAllocationEditorViewModel> PlannerAllocationEditors { get; }

  public IEnumerable<PlannerInfo> Planners { get; }

  public void UpdateParameters()
  {
    // var needsPlanning = _template.ExperimentTemplates
    //   .SelectMany(template => template.StepTemplates)
    //   .SelectMany(template => template.CommandTemplates)
    //   .SelectMany(template => template.Arguments)
    //   .Where(parameter => parameter.Planned)
    //   .Select(parameter => parameter.PlanningMetadata);
  }

  public void Save()
  {
    _template.PlannerAllocations.Clear();
    _template.PlannerAllocations.AddRange(PlannerAllocationEditors.Select(editor => editor.Save()).Where(allocation => allocation is not null));
  }
}
