using Ares.Datamodel;
using Ares.Services;
using Ares.Core.Grpc.Services;
using Google.Protobuf.WellKnownTypes;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using UI.Components.Formatting;

namespace UI.Features.DataViewer;

public partial class DataViewerViewModel : ReactiveObject
{
  private readonly AutomationService _automationClient;

  public DataViewerViewModel(AutomationService automationClient)
  {
    _automationClient = automationClient;
    SelectedSummaryStartTime = string.Empty;
    SelectedSummaryFinishTime = string.Empty;
    SelectedSummaryNumberOfExperiments = string.Empty;
    SelectedSummaryTags = string.Empty;
    SelectedSummaryNotes = string.Empty;
    AvailableSummaries = Enumerable.Empty<CampaignExecutionSummaryMetadata>();
    LoadingAvailableSumarries = false;
  }

  public async Task GetSelectedSummary()
  {
    if(SelectedSummaryMetadata is null)
      return;

    var fullSummary = await _automationClient.GetCampaignSummary(new CampaignExecutionSummaryRequest { SummaryId = SelectedSummaryMetadata.SummaryId }, null);

    if(fullSummary is null)
      throw new InvalidOperationException("Campaign Summary Request returned null!");

    FullSelectedSummary = fullSummary;
    SelectedSummaryStartTime = fullSummary.ExecutionInfo.TimeStarted.ToReadableTimestamp();
    SelectedSummaryFinishTime = fullSummary.ExecutionInfo.TimeFinished.ToReadableTimestamp();
    SelectedSummaryNumberOfExperiments = fullSummary.ExperimentSummaries.Count.ToString();
    SelectedSummaryTags = string.Join(" ", fullSummary.CampaignTags);
    SelectedSummaryNotes = fullSummary.CampaignNotes;
  }

  public async Task UpdateAvailableSummaries()
  {
    LoadingAvailableSumarries = true;
    var summaries = await _automationClient.GetAvailableCampaignExecutionSummaries(new Empty(), null);
    AvailableSummaries = summaries.AvailableCampaignSummaries;
    LoadingAvailableSumarries = false;
  }

  public async Task GetSummaryFromLinkId(string id)
  {
    await UpdateAvailableSummaries();
    SelectedSummaryMetadata = AvailableSummaries.FirstOrDefault(sum => sum.SummaryId == id);
    await GetSelectedSummary();
  }

  public void SelectedTreeItemUpdated(object? value)
  {
    SelectedExperimentSummary = null;
    SelectedStepSummary = null;
    SelectedCommandSummary = null;

    if(value is null)
      return;

    if(value is ExperimentExecutionSummary expSummary)
      SelectedExperimentSummary = expSummary;
    else if(value is StepExecutionSummary stepSummary)
      SelectedStepSummary = stepSummary;
    else if(value is CommandExecutionSummary commandExecutionSummary)
      SelectedCommandSummary = commandExecutionSummary;
  }

  [Reactive]
  public partial CampaignExecutionSummary? FullSelectedSummary { get; set; }

  [Reactive]
  public partial string SelectedSummaryStartTime { get; set; }

  [Reactive]
  public partial string SelectedSummaryFinishTime { get; set; }

  [Reactive]
  public partial string SelectedSummaryNumberOfExperiments { get; set; }

  [Reactive]
  public partial string SelectedSummaryTags { get; set; }

  [Reactive]
  public partial string SelectedSummaryNotes { get; set; }

  [Reactive]
  public partial ExperimentExecutionSummary? SelectedExperimentSummary { get; set; }

  [Reactive]
  public partial StepExecutionSummary? SelectedStepSummary { get; set; }

  [Reactive]
  public partial CommandExecutionSummary? SelectedCommandSummary { get; set; }

  [Reactive]
  public partial IEnumerable<CampaignExecutionSummaryMetadata> AvailableSummaries { get; set; }

  [Reactive]
  public partial bool LoadingAvailableSumarries { get; set; }

  public CampaignExecutionSummaryMetadata? SelectedSummaryMetadata { get; set; }
}
