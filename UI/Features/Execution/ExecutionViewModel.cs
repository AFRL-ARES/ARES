using Ares.Core.Analyzing;
using Ares.Core.Device.Providers;
using Ares.Core.Execution;
using Ares.Core.Grpc.Services;
using Ares.Core.Planning;
using Ares.Datamodel;
using Ares.Datamodel.Analyzing;
using Ares.Datamodel.Planning;
using Ares.Datamodel.Templates;
using Ares.Datamodel.Visualizing.Local;
using Ares.Services;
using DynamicData;
using Google.Protobuf.WellKnownTypes;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reactive.Linq;
using UI.Application.Notifications;
using UI.Domain.Execution;
using UI.Domain.Experiments;
using UI.Features.Visualization.ViewModels;

namespace UI.Features.Execution;

public partial class ExecutionViewModel : ReactiveObject, INotifyPropertyChanged
{
  private readonly AutomationService _automationClient;
  private readonly AnalyzerService _analyzerService;
  private readonly IAnalyzerTransactionProvider _analyzerTransactionProvider;
  public readonly ObservableCollection<CampaignTemplateSummary> CampaignTemplateSummaries = [];
  private readonly INotificationReceivingService _notificationService;
  private readonly IPlannerServiceRepo _plannerServiceRepo;
  private readonly IPlannerTransactionProvider _plannerTransactionProvider;
  private readonly IExecutionReportStore _executionReportStore;
  private readonly IAresDeviceProvider _deviceProvider;
  public event Action? StateChanged;

  private IDisposable? _experimentSubscription;
  private IDisposable? _campaignStateSubscription;

  public ExecutionViewModel(AutomationService automationClient,
    IConfiguration configuration,
    INotificationReceivingService notificationService,
    AnalyzerService analyzerService,
    IAnalyzerTransactionProvider analysisTransactionProvider,
    IPlannerServiceRepo plannerServiceRepo,
    IPlannerTransactionProvider plannerTransactionProvider,
    IExecutionReportStore executionReportStore,
    IAresDeviceProvider deviceProvider)
  {
    _automationClient = automationClient;
    _notificationService = notificationService;
    _analyzerService = analyzerService;
    _analyzerTransactionProvider = analysisTransactionProvider;
    _plannerServiceRepo = plannerServiceRepo;
    _plannerTransactionProvider = plannerTransactionProvider;
    _executionReportStore = executionReportStore;
    _deviceProvider = deviceProvider;

    PlannerAdapterInfos = [];
    AnalyzerMetrics = [];
    PlannerMetricsMap = [];
    ExperimentExecutionStatuses = [];

    this.WhenAnyValue(x => x.CurrentPlannerState)
      .Subscribe(newState =>
      {
        _ = UpdatePlannerTransactions();
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

  public async Task StartCampaign()
  {
    var executionEligibility = await _automationClient.CheckExecutionEligibility(new Empty(), null);

    if(!executionEligibility.IsEligible)
    {
      var notification = new AresNotification
      {
        NotificationSeverity = Severity.Error,
        Title = "Campaign Could Not Be Started!",
        Message = $"ARES failed to start the requested campaign: {executionEligibility.Error}",
        Timestamp = DateTime.UtcNow.ToTimestamp()
      };

      _notificationService.PushNotification(notification);
      return;
    }

    ExperimentExecutionStatuses.Clear();
    var request = new StartCampaignRequest() { UserNotes = ExecutionNotes };

    if(SelectedTags is not null)
      request.CampaignTags.AddRange(SelectedTags);

    await _automationClient.StartExecution(request, null);
    PlannerMetricsMap.Clear();
    AnalyzerMetrics.Clear();
    CurrentCampaignStartTime = DateTime.UtcNow;
    DisplayExecutionSummary = true;
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
    if(CampaignTemplate is null || CurrentAnalysisState != AnalysisState.AnalysisComplete)
      return;

    var filter = new AnalyzerTransactionRequestFilter 
    { 
      AnalyzerId = CampaignTemplate.ExperimentTemplate.AnalyzerId, 
      Start = CurrentCampaignStartTime.ToTimestamp(), 
      End = DateTime.UtcNow.ToTimestamp() 
    };

    var analyzerTransactions = await _analyzerTransactionProvider.GetAnalyzerTransactionsAsync(filter);
    var newestTransaction = analyzerTransactions.LastOrDefault();

    if(newestTransaction is null)
      return;

    OnAnalyzerTransactionReceived(newestTransaction, analyzerTransactions.Count());
  }

  private async Task UpdatePlannerTransactions()
  {
    try
    {
      if(CampaignTemplate is null || CurrentPlannerState != PlannerState.PlanningComplete)
        return;

      var usedPlanners = CampaignTemplate.ExperimentTemplate.GetAllPlannedParameters()
        .Select(p => p.PlanningMetadata.PlannerName)
        .Select(_plannerServiceRepo.GetPlannerByName)
        .Where(p => p is not null)
        .Distinct()
        .ToList();

      foreach(var planner in usedPlanners)
      {

        var transactionRequest = new PlannerTransactionRequestFilter
        {
          PlannerId = planner?.UniqueId,
          Start = CurrentCampaignStartTime.ToTimestamp(),
          End = DateTime.UtcNow.ToTimestamp()
        };

        var transactions = await _plannerTransactionProvider.GetPlanningTransactionsAsync(transactionRequest);
        var newestTransaction = transactions.LastOrDefault();

        if(newestTransaction is null)
          continue;

        OnPlannerTransactionReceived(newestTransaction, transactions.Count());
      }
    }

    catch(Exception ex)
    {
      Console.WriteLine("Dangit man");
    }
  }

  public void OnPlannerTransactionReceived(PlannerTransaction transaction, int currentTurn)
  {
    foreach(var field in transaction.PlanningResponse.PlannedParameters)
    {
      var metricName = field.ParameterName;
      var metricData = field.ParameterValue;

      if(TryGetChartableValue(metricData, out double numericValue))
      {
        if(!PlannerMetricsMap.ContainsKey(metricName))
          PlannerMetricsMap[metricName] = new List<ChartMetricPoint>();

        PlannerMetricsMap[metricName].Add(new ChartMetricPoint
        {
          ExecutionIndex = currentTurn,
          Value = numericValue
        });
      }
    }
  }

  public void OnAnalyzerTransactionReceived(AnalyzerTransaction transaction, int currentTurn)
  {
    AnalyzerMetrics.Add(new ChartMetricPoint
    {
      ExecutionIndex = currentTurn,
      Value = transaction.AnalysisResponse.Result
    }); 
  }

  public bool TryGetChartableValue(AresValue aresValue, out double result)
  {
    result = 0;
    if(aresValue == null || aresValue.KindCase == AresValue.KindOneofCase.None)
      return false;

    switch(aresValue.KindCase)
    {
      case AresValue.KindOneofCase.NumberValue:
        result = aresValue.NumberValue;
        return true;

      case AresValue.KindOneofCase.QuantityValue:
        result = aresValue.QuantityValue.Scalar;
        return true;

      case AresValue.KindOneofCase.BoolValue:
        result = aresValue.BoolValue ? 1.0 : 0.0;
        return true;

      case AresValue.KindOneofCase.StringValue:
        // Try to parse it just in case someone sent "42.5" as a string
        return double.TryParse(aresValue.StringValue, out result);

      // The un-chartable :(
      default:
        return false;
    }
  }

  public void StartWatchingTelemetry()
  {
    _experimentSubscription = _executionReportStore.ExperimentStatusObservable
        .Where(status => status is not null)
        .Subscribe(
            onNext: status => UpdateExperimentStatus(status!),
            onError: ex => Console.WriteLine($"Telemetry Error: {ex.Message}")
        );

    _campaignStateSubscription = _executionReportStore.CampaignStatusObservable
      .Where(status => status is not null)
      .Select(status => new CampaignExecutionState
      {
        CampaignId = status!.CampaignId,
        State = status.State,
        AnalysisState = status.AnalysisState,
        PlannerState = status.PlannerState
      })
      .Subscribe(
        onNext: state => UpdateCampaignStatus(state!), 
        onError: ex => Console.WriteLine($"Error Updating Campaign State: {ex.Message}"));

  }

  private void UpdateExperimentStatus(ExperimentExecutionStatus status)
  {
    var existingStatus = ExperimentExecutionStatuses.FirstOrDefault(s => s.ExperimentId == status.ExperimentId);

    if(existingStatus is null)
    {
      ExperimentExecutionStatuses.Add(status);
    }
    else
    {
      var incomingCommands = status.GetCommandExecutionStatuses();
      var existingCommands = existingStatus.GetCommandExecutionStatuses();

      foreach(var existingCommand in existingCommands)
      {
        var newCommand = incomingCommands.FirstOrDefault(c => c.CommandId == existingCommand.CommandId);
        existingCommand.State = newCommand?.State ?? ExecutionState.Undefined;
      }
    }

    StateChanged?.Invoke();
  }

  private void UpdateCampaignStatus(CampaignExecutionState state)
  {
    CampaignActive = state.IsActive();
    CampaignPaused = state.IsPaused();
    CampaignExecutionState = state.State;
    CurrentAnalysisState = state.AnalysisState;
    CurrentPlannerState = state.PlannerState;

    //TODO: FIX THIS!!!
    if(CampaignExecutionState == ExecutionState.AwaitingUser)
      _ = RequestUserConfirmation();
  }

  public Task UpdateDeviceChartA()
  {
    if(ChartConfigA is null)
    {
      ChartA = null;
      return Task.CompletedTask;
    }

    var device = _deviceProvider.GetDevice(ChartConfigA.DeviceId);

    if(device is not null)
      ChartA = new VisualizationItemViewModel(ChartConfigA, device, OnChartOneDeleteRequested, OnChartOneUpdated);
    
    return Task.CompletedTask;
  }

  public Task UpdateDeviceChartB()
  {
    if(ChartConfigB is null)
    {
      ChartB = null;
      return Task.CompletedTask;
    }

    var device = _deviceProvider.GetDevice(ChartConfigB.DeviceId);

    if(device is not null)
      ChartB = new VisualizationItemViewModel(ChartConfigB, device, OnChartTwoDeleteRequested, OnChartTwoUpdated);

    return Task.CompletedTask;
  }

  private void OnChartOneDeleteRequested(string uniqueId)
  {
    ChartA = null;
    ChartConfigA = null;
  }

  private void OnChartOneUpdated(string uniqueId, DeviceVisualizationConfig config)
  {
    ChartConfigA = config;
    UpdateDeviceChartA();
  }

  private void OnChartTwoDeleteRequested(string uniqueId)
  {
    ChartB = null;
    ChartConfigB = null;
  }

  private void OnChartTwoUpdated(string uniqueId, DeviceVisualizationConfig config)
  {
    ChartConfigB = config;
    UpdateDeviceChartB();
  }

  public void Dispose()
  {
    _experimentSubscription?.Dispose();
    _campaignStateSubscription?.Dispose();
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
  [Reactive]
  public Dictionary<string, List<ChartMetricPoint>> PlannerMetricsMap { get; private set; }
  [Reactive]
  public partial List<ChartMetricPoint> AnalyzerMetrics { get; private set; }
  [Reactive]
  public partial IList<ExperimentExecutionStatus> ExperimentExecutionStatuses { get; private set; }
  [Reactive]
  public partial VisualizationItemViewModel? ChartA { get; private set; }
  [Reactive]
  public partial DeviceVisualizationConfig? ChartConfigA { get; set; }
  [Reactive]
  public partial DeviceVisualizationConfig? ChartConfigB { get; set; }
  [Reactive]
  public partial VisualizationItemViewModel? ChartB { get; private set; }
  public uint ExperimentsToRun { get; set; }
  public string ExecutionNotes { get; set; } = string.Empty;
  public CampaignExecutionSummary? TestCampaignExecutionSummary { get; private set; }
  public IEnumerable<CampaignExecutionSummaryMetadata>? TestCampaignResultMetadata { get; private set; }
  public bool DisplayExecutionSummary { get; set; }
  public List<AresCampaignTag> AvailableTags { get; set; } = [];
  public List<AresCampaignTag> SelectedTags { get; set; } = [];
  public string? NewTagName { get; set; }
  public DateTime CurrentCampaignStartTime { get; set; }
}

public class ChartMetricPoint
{
  public int ExecutionIndex { get; set; }

  public double Value { get; set; }
}