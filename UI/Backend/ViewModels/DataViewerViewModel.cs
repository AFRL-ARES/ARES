using Ares.Datamodel;
using Ares.Services;
using Google.Protobuf.WellKnownTypes;
using Radzen;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using UI.Backend.Helpers;

namespace UI.Backend.ViewModels;

public class DataViewerViewModel : ReactiveObject
{
  private readonly AresAutomation.AresAutomationClient _automationClient;

  public DataViewerViewModel(AresAutomation.AresAutomationClient automationClient)
  {
    _automationClient = automationClient;
  }

  public async Task GetSelectedSummary()
  {
    if(SelectedSummaryMetadata is null)
      return;

    var fullSummary = await _automationClient.GetCampaignSummaryAsync(new CampaignExecutionSummaryRequest { SummaryId = SelectedSummaryMetadata.SummaryId });

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
    var summaries = await _automationClient.GetAvailableCampaignExecutionSummariesAsync(new Empty());
    AvailableSummaries = summaries.AvailableCampaignSummaries;
    LoadingAvailableSumarries = false;
  }

  public async Task GetSummaryFromLinkId(string id)
  {
    await UpdateAvailableSummaries();
    SelectedSummaryMetadata = AvailableSummaries.FirstOrDefault(sum => sum.SummaryId == id);
    await GetSelectedSummary();
  }

  public void SelectedTreeItemUpdated(TreeEventArgs args)
  {
    SelectedExperimentSummary = null;
    SelectedStepSummary = null;
    SelectedCommandSummary = null;

    if(args.Value is null)
      return;

    if(args.Value is ExperimentExecutionSummary expSummary)
      SelectedExperimentSummary = expSummary;

    else if(args.Value is StepExecutionSummary stepSummary)
      SelectedStepSummary = stepSummary;

    else if(args.Value is CommandExecutionSummary commandExecutionSummary)
      SelectedCommandSummary = commandExecutionSummary;
  }

  [Reactive]
  public CampaignExecutionSummary? FullSelectedSummary { get; set; }

  [Reactive]
  public string SelectedSummaryStartTime { get; set; } = string.Empty;

  [Reactive]
  public string SelectedSummaryFinishTime { get; set; } = string.Empty;

  [Reactive]
  public string SelectedSummaryNumberOfExperiments { get; set; } = string.Empty;

  [Reactive]
  public string SelectedSummaryTags { get; set; } = string.Empty;

  [Reactive]
  public string SelectedSummaryNotes { get; set; } = string.Empty;

  [Reactive]
  public ExperimentExecutionSummary? SelectedExperimentSummary { get; set; }

  [Reactive]
  public StepExecutionSummary? SelectedStepSummary { get; set; }

  [Reactive]
  public CommandExecutionSummary? SelectedCommandSummary { get; set; }

  [Reactive]
  public IEnumerable<CampaignExecutionSummaryMetadata> AvailableSummaries { get; set; } = Enumerable.Empty<CampaignExecutionSummaryMetadata>();

  [Reactive]
  public bool LoadingAvailableSumarries { get; set; } = false;

  public CampaignExecutionSummaryMetadata? SelectedSummaryMetadata { get; set; }
}