using Ares.Services;
using Ares.Core.Grpc.Services;
using DynamicData;
using Google.Protobuf.WellKnownTypes;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using UI.Application.Execution;

namespace UI.Features.ExecutionHistory;

public partial class ExecutionHistoryViewModel : ReactiveObject
{
  private readonly AutomationService _automationClient;

  public ExecutionHistoryViewModel(AutomationService automationClient)
  {
    _automationClient = automationClient;
    CampaignSummaries = [];
    LoadingExecutionHistory = false;
  }

  public async Task UpdateExecutionSummaries()
  {
    LoadingExecutionHistory = true;
    CampaignSummaries.Clear();
    var response = await _automationClient.GetAvailableCampaignExecutionSummaries(new Empty(), null);
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