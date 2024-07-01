using Ares.Messaging;
using ReactiveUI;

namespace UI.Backend.ViewModels.Automation.CampaignEdit;

public class PlannerAllocationEditorViewModel : ReactiveObject
{

  public PlannerAllocationEditorViewModel(ParameterMetadata metadata, PlannerInfo? plannerInfo, IEnumerable<PlannerInfo> planners)
  {
    var plannerArray = planners.ToArray();
    if (plannerInfo is not null)
      PlannerInfoId = plannerArray.First(info => info.Type == plannerInfo.Type && info.Version == plannerInfo.Version && info.Name == plannerInfo.Name).UniqueId;

    ParameterMetadata = metadata;
    Planners = plannerArray;
  }

  public string? PlannerInfoId { get; set; }
  public ParameterMetadata ParameterMetadata { get; }
  public IEnumerable<PlannerInfo> Planners { get; }

  public PlannerAllocation? Save()
  {
    if (PlannerInfoId is null)
      return null;

    var allocation = new PlannerAllocation
    {
      Parameter = ParameterMetadata,
      Planner = Planners.First(info => info.UniqueId == PlannerInfoId),
      UniqueId = Guid.NewGuid().ToString()
    };

    return allocation;
  }
}
