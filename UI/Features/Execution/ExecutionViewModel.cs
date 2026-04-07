using Ares.Datamodel;
using Ares.Datamodel.Analyzing;
using Ares.Datamodel.Planning;
using Ares.Datamodel.Templates;
using Ares.Services;
using Ares.Core.Grpc.Services;
using DynamicData;
using Google.Protobuf.WellKnownTypes;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System.Collections.ObjectModel;
using System.ComponentModel;
using UI.Application.Notifications;
using UI.Domain.Experiments;
using Ares.Core.Analyzing;
using System.Reactive.Linq;

namespace UI.Features.Execution;

public partial class ExecutionViewModel : ReactiveObject, INotifyPropertyChanged
{
  private readonly AutomationService _automationClient;
  private readonly AnalyzerService _analyzerService;
  private readonly IAnalyzerTransactionProvider _analyzerTransactionProvider;
  public readonly ObservableCollection<CampaignTemplateSummary> CampaignTemplateSummaries = [];
  private readonly INotificationReceivingService _notificationService;

  public ExecutionViewModel(AutomationService automationClient,
    IConfiguration configuration,
    INotificationReceivingService notificationService,
    AnalyzerService analyzerService,
    IAnalyzerTransactionProvider analysisTransactionProvider)
  {
    _automationClient = automationClient;
    _notificationService = notificationService;
    _analyzerService = analyzerService;
    _analyzerTransactionProvider = analysisTransactionProvider;
    PlannerAdapterInfos = [];

    this.WhenAnyValue(x => x.CurrentPlannerState)
      .Subscribe(newState =>
      {
        Console.WriteLine($"Planning state has changed to: {newState}");
      });

    this.WhenAnyValue(x => x.CurrentAnalysisState)
      .Subscribe(newState =>
      {
        _ = UpdateAnalysisTransactions();
      });
  }

  public async Task<bool> EnsureStopConditionSet()
  {
    await GetCurrentStopCondition();
    return CurrentStopCondition is not null;
  }

  public async Task RefreshCampaigns()
  {
    var campaigns = await _automationClient.GetAllCampaigns(new GetAllCampaignsRequest(), null);
    CampaignTemplateSummaries.Clear();
    CampaignTemplateSummaries.AddRange(campaigns.Campaigns);
  }

  public async Task SelectCampaignTemplate(object? templateSummary)
  {
    if(templateSummary is null || templateSummary is not CampaignTemplateSummary campaignTemplateSummary)
      return;

    CampaignTemplate = await _automationClient.GetSingleCampaign(new CampaignRequest { UniqueId = campaignTemplateSummary.UniqueId }, null);
    await _automationClient.SetCampaignForExecution(new CampaignRequest { UniqueId = CampaignTemplate.UniqueId }, null);
    _ = UpdateCurrentTemplate();
    DisplayExecutionSummary = false;
  }

  public async Task UpdateCurrentTemplate()
  {
    var currentTemplateOpt = await _automationClient.GetCurrentlySelectedCampaign(new Empty(), null);
    CampaignTemplate = currentTemplateOpt.Value;
    if(CampaignTemplate is null)
      return;

    PlannerAdapterInfos = CampaignTemplate.ExperimentTemplate.GetAllPlannedParameters()
    .Select(parameter => parameter.PlanningMetadata)
    .Select(metadata => CampaignTemplate.PlannerAllocations
    .FirstOrDefault(allocation => allocation.Parameter.Equals(metadata))?.Planner).ToHashSet();

    var analyzerId = CampaignTemplate.ExperimentTemplate.AnalyzerId;

    if(analyzerId is not null)
    {
      var request = new AnalyzerInfoRequest();
      request.AnalyzerId = analyzerId;
      var response = await _analyzerService.GetInfo(request, null);
      AnalyzerInfo = response.Info;
    }
  }

  public async Task SetDesiredAnalysis()
  {
    await _automationClient.SetAnalysisResultStopCondition(
      new AnalysisResultCondition { DesiredResult = DesiredResult, Leeway = DesiredLeeway }, null);
    CurrentStopCondition = await GetCurrentStopCondition();
  }

  public Task<ExperimentStopConditionResponse> GetCurrentStopCondition()
  {
    return _automationClient.GetActiveStopCondition(new Empty(), null);
  }

  public Task<GetReplanRateResponse> GetCurrentReplanRate()
  {
    return _automationClient.GetReplanRate(new Empty(), null);
  }

  public async Task<CampaignExecutionStatus?> GetCampaignExecutionStatus()
  {
    var response = await _automationClient.GetCampaignExecutionStatus(new Empty(), null);
    return response.Status;
  }

  public async Task SetExperimentsToRun()
  {
    await _automationClient.SetNumExperimentsStopCondition(new NumExperimentsCondition { NumExperiments = ExperimentsToRun }, null);
    CurrentStopCondition = await GetCurrentStopCondition();
  }

  public async Task SetReplanRate()
  {
    await _automationClient.SetReplanRate(new ReplanRate { ReplanRate_ = DesiredReplanRate }, null);
    var replanRateResponse = await GetCurrentReplanRate();
    DesiredReplanRate = replanRateResponse.ReplanRate;
  }

  public Task StopCampaign()
    => _automationClient.StopExecution(new Empty(), null);

  public Task PauseCampaign()
    => _automationClient.PauseExecution(new Empty(), null);

  public Task ResumeCampaign()
    => _automationClient.ResumeExecution(new Empty(), null);

  public async Task ExecutionNotesUploaded(Stream fileStream)
  {
    using var reader = new StreamReader(fileStream);
    try
    {
      ExecutionNotes = await reader.ReadToEndAsync();
    }
    catch(Exception ex)
    {
      var notification = new AresNotification
      {
        NotificationSeverity = Severity.Error,
        Title = "Failed to Upload Experiment Notes",
        Message = $"ARES failed to read the uploaded experiment notes file. {ex.Message}",
        Timestamp = DateTime.UtcNow.ToTimestamp()
      };

      _notificationService.PushNotification(notification);
    }
  }

  public Task RequestUserConfirmation()
  {
    var notification = new AresNotification();
    notification.NotificationSeverity = Severity.Info;
    notification.Title = "User Confirmation Required to Proceed";
    notification.Message = $"ARES has paused it's current experiment awaiting user input. Press the play button to continue experimenting.";
    notification.Timestamp = DateTime.UtcNow.ToTimestamp();
    notification.Loiter = true;

    _notificationService.PushNotification(notification);

    return Task.CompletedTask;
  }

  public async Task AddTag()
  {
    if(NewTagName is not null && AvailableTags.Any(t => t.TagName == NewTagName))
    {
      var notification = new AresNotification();
      notification.NotificationSeverity = Severity.Info;
      notification.Title = $"Could Not Add {NewTagName} Tag";
      notification.Message = "ARES could not add a new experiment tag because it matched one that already existed!";
      notification.Timestamp = DateTime.UtcNow.ToTimestamp();
      _notificationService.PushNotification(notification);
      return;
    }

    var newProtoTag = new AresCampaignTag { TagName = NewTagName, UniqueId = Guid.NewGuid().ToString() };
    var currentTagCount = AvailableTags.Count;
    var request = new TagRequest();
    request.Tag = newProtoTag;
    var tags = await _automationClient.AddTag(request, null);

    if(tags.AvailableTags.Count == currentTagCount + 1)
    {
      var notification = new AresNotification
      {
        NotificationSeverity = Severity.Success,
        Title = $"Successfully Added {NewTagName} Tag",
        Message = "ARES has successfully added a new experiment tag, and it is now available for use",
        Timestamp = DateTime.UtcNow.ToTimestamp()
      };
      _notificationService.PushNotification(notification);
    }

    else
    {
      var notification = new AresNotification
      {
        NotificationSeverity = Severity.Error,
        Title = $"Failed to Add {NewTagName} Tag",
        Message = "ARES failed to add a new experiment tag",
        Timestamp = DateTime.UtcNow.ToTimestamp()
      };
      _notificationService.PushNotification(notification);
    }

    AvailableTags = tags.AvailableTags.ToList();
    NewTagName = string.Empty;
  }

  public async Task RemoveTag(AresCampaignTag? aresTag)
  {
    if(aresTag is null)
      return;

    var request = new TagRequest() { Tag = aresTag };
    var tags = await _automationClient.RemoveTag(request, null);

    AvailableTags = tags.AvailableTags.ToList();

    SelectedTags.Remove(aresTag);
  }

  public async Task GetAllTags()
  {
    var tags = await _automationClient.GetAllTags(new Empty(), null);
    AvailableTags = tags.AvailableTags.ToList();
  }

  private async Task UpdateAnalysisTransactions()
  {
    if(CampaignTemplate is not null)
    {
      try
      {
        var bleh = new AnalyzerTransactionRequestFilter { AnalyzerId = CampaignTemplate.ExperimentTemplate.AnalyzerId, Start = CurrentCampaignStartTime?.ToTimestamp(), End = DateTime.UtcNow.ToTimestamp() };
        var blah = await _analyzerTransactionProvider.GetAnalyzerTransactionsAsync(bleh);
      }

      catch(Exception e)
      {
        Console.WriteLine("lol oops");
      }
    }
  }

  private async Task UpdatePlannerTransactions()
  {
    if(CampaignTemplate is not null)
    {
      try
      {
        var bleh = new PlannerTransactionRequestFilter { PlannerId = }
      }
    }
  }

  [Reactive]
  public partial ExperimentStopConditionResponse? CurrentStopCondition { get; set; }
  public double DesiredResult { get; set; }
  public double DesiredLeeway { get; set; }
  public int DesiredReplanRate { get; set; } = 1;
  [Reactive]
  public partial bool CampaignActive { get; set; }
  [Reactive]
  public partial bool CampaignPaused { get; set; }
  [Reactive]
  public partial CampaignTemplateSummary? SelectedTemplateSummary { get; set; }
  [Reactive]
  public partial CampaignTemplate? CampaignTemplate { get; set; }
  [Reactive]
  public partial ExecutionState? CampaignExecutionState { get; set; }
  [Reactive]
  public partial AnalysisState? CurrentAnalysisState { get; set; }
  [Reactive]
  public partial PlannerState? CurrentPlannerState { get; set; }
  [Reactive]
  public partial ExperimentExecutionStatus? ExperimentStatus { get; private set; }
  [Reactive]
  public partial HashSet<PlannerServiceInfo?> PlannerAdapterInfos { get; set; }
  [Reactive]
  public partial AnalyzerInfo? AnalyzerInfo { get; set; }
  public uint ExperimentsToRun { get; set; }
  public string ExecutionNotes { get; set; } = string.Empty;
  public CampaignExecutionSummary? TestCampaignExecutionSummary { get; private set; }
  public IEnumerable<CampaignExecutionSummaryMetadata>? TestCampaignResultMetadata { get; private set; }
  public bool DisplayExecutionSummary { get; set; }
  public List<AresCampaignTag> AvailableTags { get; set; } = [];
  public List<AresCampaignTag> SelectedTags { get; set; } = [];
  public string? NewTagName { get; set; }
  public DateTime? CurrentCampaignStartTime { get; set; }
}


