using ReactiveUI;
using System.Collections.ObjectModel;
using Ares.Datamodel.Templates;
using Ares.Services;
using Ares.Datamodel.Planning;
using Ares.Core.Grpc.Services;
using UI.Application.Notifications;

namespace UI.Features.CampaignEdit.ViewModels;

public class PlanningViewModel : ReactiveObject
{
  private readonly CampaignTemplate _template;
  private readonly PlannerService _client;
  private readonly IUiNotificationService _notificationService;

  public PlanningViewModel(CampaignTemplate template, 
    IEnumerable<PlannerServiceInfo> plannerAdapters, 
    PlannerService client,
    IUiNotificationService notificationService)
  {
    _template = template;
    _client = client;
    _notificationService = notificationService;
    PlannerAdapters = new ReadOnlyCollection<PlannerServiceInfo>(plannerAdapters.ToList());
    PlannerAllocationEditors = template.PlannableParameters.Select(metadata => new PlannerAllocationEditorViewModel(metadata, template.PlannerAllocations.FirstOrDefault(allocation => allocation.Parameter.Equals(metadata))?.Planner, PlannerAdapters, client, notificationService)).ToArray();
  }

  public IEnumerable<PlannerAllocationEditorViewModel> PlannerAllocationEditors { get; private set; }

  public IEnumerable<PlannerServiceInfo> PlannerAdapters { get; }

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
    .FirstOrDefault(allocation => allocation.Parameter.Equals(metadata))?.Planner, PlannerAdapters, _client, _notificationService))
    .ToArray();
  }
}


