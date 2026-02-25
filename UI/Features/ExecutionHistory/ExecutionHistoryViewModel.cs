using Ares.Services;
using DynamicData;
using Google.Protobuf.WellKnownTypes;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using UI.Application.Execution;

namespace UI.Features.ExecutionHistory;

public partial class ExecutionHistoryViewModel : ReactiveObject
{
  private readonly AresAutomation.AresAutomationClient _automationClient;

  public ExecutionHistoryViewModel(AresAutomation.AresAutomationClient automationClient)
  {
    _automationClient = automationClient;
    CampaignSummaries = [];
    LoadingExecutionHistory = false;
  }

  public async Task UpdateExecutionSummaries()
  {
    LoadingExecutionHistory = true;
    CampaignSummaries.Clear();
    CampaignDisplaySummaries.Clear();
    var response = await _automationClient.GetAvailableCampaignExecutionSummariesAsync(new Empty());
    CampaignSummaries.AddRange(response.AvailableCampaignSummaries);
    CampaignDisplaySummaries.AddRange(response.AvailableCampaignSummaries.Select(p => 
    new CampaignSummaryDisplay()
    {
      SummaryId = p.SummaryId,
      CampaignName = p.CampaignName,
      NumExperiments = (int)p.NumExperiments,
      CompletionTimeDateTime = p.CompletionTime.ToDateTime(),
      OriginalTimestamp = p.CompletionTime
    }));
    LoadingExecutionHistory = false;
  }

  [Reactive]
  public partial IList<CampaignExecutionSummaryMetadata> CampaignSummaries { get; set; }

  [Reactive]
  public IList<CampaignSummaryDisplay> CampaignDisplaySummaries { get; set; } = [];

  [Reactive]
  public partial bool LoadingExecutionHistory { get; set; }
}