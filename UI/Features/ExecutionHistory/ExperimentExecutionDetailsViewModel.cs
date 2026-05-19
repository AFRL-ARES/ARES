using Ares.Datamodel;
using Ares.Services;
using Ares.Core.Grpc.Services;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using UI.Components.Formatting;

namespace UI.Features.ExecutionHistory;

public partial class ExperimentExecutionDetailsViewModel : ReactiveObject
{
  private readonly AutomationService _automationClient;

  public ExperimentExecutionDetailsViewModel(AutomationService automationClient)
  {
    _automationClient = automationClient;
    SelectedSummaryStartTime = string.Empty;
    SelectedSummaryFinishTime = string.Empty;
    SelectedSummaryNumberOfExperiments = string.Empty;
    SelectedSummaryTags = string.Empty;
    SelectedSummaryNotes = string.Empty;
    LoadingSelectedSummary = false;
  }

  public async Task GetSelectedSummary(CampaignExecutionSummaryMetadata? selectedSummaryMetadata)
  {
    ClearSelectedSummary();

    if(selectedSummaryMetadata is null)
      return;

    try
    {
      LoadingSelectedSummary = true;
      var fullSummary = await _automationClient.GetCampaignSummary(new CampaignExecutionSummaryRequest { SummaryId = selectedSummaryMetadata.SummaryId }, null);

      if(fullSummary is null)
        throw new InvalidOperationException("Campaign Summary Request returned null!");

      FullSelectedSummary = fullSummary;
      SelectedSummaryStartTime = fullSummary.ExecutionInfo.TimeStarted.ToReadableTimestamp();
      SelectedSummaryFinishTime = fullSummary.ExecutionInfo.TimeFinished.ToReadableTimestamp();
      SelectedSummaryNumberOfExperiments = fullSummary.ExperimentSummaries.Count.ToString();
      SelectedSummaryTags = string.Join(" ", fullSummary.CampaignTags);
      SelectedSummaryNotes = fullSummary.CampaignNotes;
    }
    finally
    {
      LoadingSelectedSummary = false;
    }
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

  private void ClearSelectedSummary()
  {
    FullSelectedSummary = null;
    SelectedSummaryStartTime = string.Empty;
    SelectedSummaryFinishTime = string.Empty;
    SelectedSummaryNumberOfExperiments = string.Empty;
    SelectedSummaryTags = string.Empty;
    SelectedSummaryNotes = string.Empty;
    SelectedExperimentSummary = null;
    SelectedStepSummary = null;
    SelectedCommandSummary = null;
    LoadingSelectedSummary = false;
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
  public partial bool LoadingSelectedSummary { get; set; }
}
