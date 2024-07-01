using System.Collections.ObjectModel;
using Ares.Messaging;
using DynamicData;
using Google.Protobuf.WellKnownTypes;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using UI.Backend.Extensions;
using UI.Backend.Helpers;

namespace UI.Backend.ViewModels.Automation;

public class ExecutionViewModel : ReactiveObject
{

  private readonly AresAutomation.AresAutomationClient _automationClient;

  public readonly ObservableCollection<CampaignTemplate> Templates = new();

  public ExecutionViewModel(AresAutomation.AresAutomationClient automationClient)
  {
    _automationClient = automationClient;
  }

  [Reactive]
  public CampaignTemplate? CampaignTemplate { get; set; }

  [Reactive]
  public ExperimentExecutionStatus? ExperimentStatus { get; private set; }

  public HashSet<PlannerInfo?> PlannerInfos { get; set; } = new();

  public uint ExperimentsToRun { get; set; }

  public CampaignResult? TestCampaignResult { get; private set; }
  public IEnumerable<CampaignResultMetadata>? TestCampaignResultMetadata { get; private set; }

  public async Task RefreshCampaigns()
  {
    var campaigns = await _automationClient.GetAllCampaignsAsync(new Empty());
    Templates.Clear();
    Templates.AddRange(campaigns.CampaignTemplates);
  }

  public void SelectCampaignTemplate(object template)
  {
    if (template is not CampaignTemplate campaignTemplate)
      return;

    CampaignTemplate = campaignTemplate;
    _automationClient.SetCampaignForExecution(new CampaignRequest { UniqueId = campaignTemplate.UniqueId });
    _ = UpdateCurrentTemplate();
  }

  [Reactive]
  public bool CampaignActive { get; set; }
  [Reactive]
  public bool CampaignPaused { get; set; }

  public bool IsCampaignActive()
  {
    if (ExperimentStatus is null)
      return false;

    return ExperimentStatus.IsActive();
  }

  public bool IsCampaignPaused()
  {
    if (ExperimentStatus is null)
      return false;

    return ExperimentStatus.IsPaused();
  }

  public async Task UpdateCurrentTemplate()
  {
    var currentTemplateOpt = await _automationClient.GetCurrentlySelectedCampaignAsync(new Empty());
    CampaignTemplate = currentTemplateOpt.Value;
    if (CampaignTemplate is null)
      return;

    PlannerInfos = CampaignTemplate.ExperimentTemplates.First().GetAllPlannedParameters().Select(parameter => parameter.PlanningMetadata).Select(metadata => CampaignTemplate.PlannerAllocations.FirstOrDefault(allocation => allocation.Parameter.Equals(metadata))?.Planner).ToHashSet();
  }

  public async Task SetDesiredAnalysis()
  {
    await _automationClient.SetAnalysisResultStopConditionAsync(
      new AnalysisResultCondition { DesiredResult = DesiredResult, Leeway = DesiredLeeway }).ResponseAsync;
    CurrentStopCondition = await GetCurrentStopCondition();
  }

  public Task<ExperimentStopConditionResponse> GetCurrentStopCondition()
  {
    return _automationClient.GetActiveStopConditionAsync(new Empty()).ResponseAsync;
  }

  [Reactive]
  public ExperimentStopConditionResponse? CurrentStopCondition { get; set; }

  public double DesiredResult { get; set; }

  public double DesiredLeeway { get; set; }

  public async Task<CampaignExecutionStatus?> GetCampaignExecutionStatus()
  {
    var response = await _automationClient.GetCampaignExecutionStatusAsync(new Empty());
    return response.Status;
  }

  public async Task SetExperimentsToRun()
  {
    await _automationClient.SetNumExperimentsStopConditionAsync(new NumExperimentsCondition { NumExperiments = ExperimentsToRun });
    CurrentStopCondition = await GetCurrentStopCondition();
  }

  public Task StopCampaign()
    => _automationClient.StopExecutionAsync(new Empty()).ResponseAsync;

  public Task PauseCampaign()
    => _automationClient.PauseExecutionAsync(new Empty()).ResponseAsync;

  public Task ResumeCampaign()
    => _automationClient.ResumeExecutionAsync(new Empty()).ResponseAsync;

  public async Task GetAvailableCampaignResults()
  {
    var response = await _automationClient.GetAvailableCampaignResultsAsync(new Empty());
    TestCampaignResultMetadata = response.AvailableCampaignResults.ToArray();
  }

  public async Task GetRandomCampaignResult()
  {
    var response = await _automationClient.GetAvailableCampaignResultsAsync(new Empty());
    if (!response.AvailableCampaignResults.Any())
      return;

    var idx = Random.Shared.Next(response.AvailableCampaignResults.Count - 1);
    var result = response.AvailableCampaignResults.ElementAt(idx);
    TestCampaignResult = await _automationClient.GetCampaignResultAsync(new CampaignResultRequest { ResultId = result.ResultId });
  }
}
