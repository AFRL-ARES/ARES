using Ares.Core.Device.Providers;
using Ares.Core.Execution;
using Ares.Core.Grpc.Services;
using Ares.Core.Visualization.Helpers;
using Ares.Datamodel;
using Ares.Datamodel.Analyzing;
using Ares.Datamodel.Extensions;
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
  public readonly ObservableCollection<CampaignTemplateSummary> CampaignTemplateSummaries = [];
  private readonly INotificationReceivingService _notificationService;
  private readonly IExecutionReportStore _executionReportStore;
  private readonly IAresDeviceProvider _deviceProvider;
  public event Action? StateChanged;

  private IDisposable? _experimentSubscription;
  private IDisposable? _campaignStateSubscription;

  public ExecutionViewModel(AutomationService automationClient,
    IConfiguration configuration,
    INotificationReceivingService notificationService,
    AnalyzerService analyzerService,
    IExecutionReportStore executionReportStore,
    IAresDeviceProvider deviceProvider)
  {
    _automationClient = automationClient;
    _notificationService = notificationService;
    _analyzerService = analyzerService;
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
    if(CampaignActive)
      return;

    if(templateSummary is null || templateSummary is not CampaignTemplateSummary campaignTemplateSummary)
      return;

    CampaignTemplate = await _automationClient.GetSingleCampaign(new CampaignRequest { UniqueId = campaignTemplateSummary.UniqueId }, null);
    
    if(CampaignTemplate is not null)
    {
      await _automationClient.SetCampaignForExecution(new CampaignRequest { UniqueId = CampaignTemplate.UniqueId }, null);
      _ = UpdateCurrentTemplate();
    }

    else
      SelectedTemplateSummary = null;

    SelectedExecutionTabIndex = 0;
    await RefreshCampaignSetup();
  }

  public async Task UpdateCurrentTemplate()
  {
    var currentTemplateOpt = await _automationClient.GetCurrentlySelectedCampaign(new Empty(), null);
    CampaignTemplate = currentTemplateOpt.Value;
    if(CampaignTemplate is null)
      return;

    AnalyzerInfo = null;
    PlannerAdapterInfos = CampaignTemplate.ExperimentTemplate.GetAllPlannedParameters()
    .Select(parameter => parameter.GetPlanningMetadata())
    .Select(metadata => CampaignTemplate.PlannerAllocations
    .FirstOrDefault(allocation => allocation.Parameter.Equals(metadata))?.Planner)
    .Where(info => info is not null)
    .ToHashSet();

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
    ActiveStopConditionMode = ExecutionStopConditionMode.AnalyzerResult;
    await RefreshExecutionEligibility();
    StateChanged?.Invoke();
  }

  public Task<ExperimentStopConditionResponse> GetCurrentStopCondition()
  {
    return _automationClient.GetActiveStopCondition(new Empty(), null);
  }

  public Task<ReplicateRate> GetCurrentReplicateRate()
  {
    return _automationClient.GetReplicateRate(new Empty(), null);
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
    ActiveStopConditionMode = ExecutionStopConditionMode.NumExperiments;
    await RefreshExecutionEligibility();
    StateChanged?.Invoke();
  }

  public async Task SetReplanRate()
  {
    await _automationClient.SetReplicateRate(new ReplicateRate { ReplicateRate_ = DesiredReplicationRate }, null);
    var replanRateResponse = await GetCurrentReplicateRate();
    DesiredReplicationRate = replanRateResponse.ReplicateRate_;
  }

  public async Task StartCampaign()
  {
    await ApplyActiveStopCondition();

    if(!CampaignActive)
      await SetReplanRate();

    var executionEligibility = await _automationClient.CheckExecutionEligibility(new Empty(), null);
    LastExecutionEligibility = executionEligibility;

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
    SelectedExecutionTabIndex = 1;
  }

  public Task StartConfiguredCampaign()
    => StartCampaign();

  public Task StopCampaign()
    => _automationClient.StopExecution(new Empty(), null);

  public Task PauseCampaign()
    => _automationClient.PauseExecution(new Empty(), null);

  public Task ResumeCampaign()
    => _automationClient.ResumeExecution(new Empty(), null);

  public Task SubmitUserDecision(ErrorHandling decision)
    => _automationClient.SubmitUserDecision(new UserDecisionRequest { Decision = decision }, null);

  public Task ApplyActiveStopCondition()
  {
    return ActiveStopConditionMode switch
    {
      ExecutionStopConditionMode.AnalyzerResult => SetDesiredAnalysis(),
      _ => SetExperimentsToRun()
    };
  }

  private async Task RefreshExecutionEligibility()
  {
    LastExecutionEligibility = await _automationClient.CheckExecutionEligibility(new Empty(), null);
  }

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

  public async Task UpdateAnalysisTransactions()
  {
    if(CampaignTemplate is null || CurrentAnalysisState != AnalysisState.AnalysisComplete)
      return;

    var analyzerTransactions = await _automationClient.GetLatestAnalyzerTransactions();
    var newestTransaction = analyzerTransactions.LastOrDefault();

    if(newestTransaction is null)
      return;

    OnAnalyzerTransactionReceived(newestTransaction, analyzerTransactions.Count());
  }

  public async Task UpdatePlannerTransactions()
  {
    if(CurrentPlannerState != PlannerState.PlanningComplete)
      return;

    var plannerTransactions = await _automationClient.GetLatestPlanningTransactions();

    foreach(var transactionList in plannerTransactions)
    {
      if(transactionList is not null)
      {
        var newestTransaction = transactionList.LastOrDefault();

        if(newestTransaction is null)
          continue;

        OnPlannerTransactionReceived(newestTransaction, transactionList.Count());
      }
    }
  }

  public void OnPlannerTransactionReceived(PlannerTransaction transaction, int currentTurn)
  {
    foreach(var field in transaction.PlanningResponse.PlannedParameters)
    {
      var metricName = field.ParameterName;
      var metricData = field.ParameterValue;
      var matchingParam = transaction.PlanningRequest.PlanningParameters.FirstOrDefault(p => p.ParameterName == field.ParameterName);

      if(TryGetChartableValue(metricData, out double numericValue) && matchingParam is not null)
      {
        var minBound = matchingParam.MinimumValue;
        var maxBound = matchingParam.MaximumValue;
        var normalizedValue = 0.0;

        if(maxBound > minBound)
          normalizedValue = ((numericValue - minBound) / (maxBound - minBound)) * 100;

        if(!PlannerMetricsMap.ContainsKey(metricName))
          PlannerMetricsMap[metricName] = new List<ChartMetricPoint>();

        PlannerMetricsMap[metricName].Add(new ChartMetricPoint
        {
          ExecutionIndex = currentTurn,
          PlotValue = normalizedValue,  // Charting Value
          RawValue = numericValue       // Tooltip Display Value
        });
      }
    }
  }

  public void OnAnalyzerTransactionReceived(AnalyzerTransaction transaction, int currentTurn)
  {
    foreach(var objective in transaction.AnalysisResponse.Objectives)
    {
      var found = objective.ObjectiveValue.TryGetNumericValue(out var numericValue);
      if(!found)
        return;

      if(!AnalyzerMetrics.ContainsKey(objective.ObjectiveName))
        AnalyzerMetrics[objective.ObjectiveName] = new List<ChartMetricPoint>();

      AnalyzerMetrics[objective.ObjectiveName].Add(new ChartMetricPoint
      {
        ExecutionIndex = currentTurn,
        RawValue = numericValue,
        PlotValue = numericValue
      });      
    }
  }

  public bool TryGetChartableValue(AresValue aresValue, out double result)
  {
    result = 0;
    if(aresValue == null || aresValue.KindCase == AresValue.KindOneofCase.None)
      return false;

    switch(aresValue.KindCase)
    {
      case AresValue.KindOneofCase.NumberValue:
      case AresValue.KindOneofCase.FloatValue:
      case AresValue.KindOneofCase.IntValue:
        return aresValue.TryGetNumericValue(out result);

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
      ExperimentExecutionStatuses.Add(status);
 
      
    else
    {
      var incomingCommands = status.GetCommandExecutionStatuses();
      var existingCommands = existingStatus.GetCommandExecutionStatuses();

      foreach(var existingCommand in existingCommands)
      {
        var newCommand = incomingCommands.FirstOrDefault(c => c.CommandId == existingCommand.CommandId);
        if(newCommand is not null)
        {
          existingCommand.State = newCommand.State;
          existingCommand.StatusMessage = newCommand.StatusMessage;
          existingCommand.Result = newCommand.Result;
          existingCommand.VariableName = newCommand.VariableName;
        }
      }
    }

    ExtractCommandVariables(status);
    StateChanged?.Invoke();

    this.RaisePropertyChanged(nameof(CurrentCommand));
    this.RaisePropertyChanged(nameof(CompletedExperimentCount));
    this.RaisePropertyChanged(nameof(ActiveExperimentNumber));
    this.RaisePropertyChanged(nameof(CurrentOutputVariables));
  }

  private void ExtractCommandVariables(ExperimentExecutionStatus status)
  {
    CurrentOutputVariables.Clear();

    var dictionary = new Dictionary<string, AresValue>();

    foreach(var step in status.StepExecutionStatuses)
    {
      foreach(var cmd in step.CommandExecutionStatuses)
      {
        if(!string.IsNullOrWhiteSpace(cmd.VariableName))
          dictionary.Add(cmd.VariableName, cmd.Result);
      }
    }

    CurrentOutputVariables = dictionary;
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

    //if(CampaignActive)
    //  SelectedExecutionTabIndex = 1;

    StateChanged?.Invoke();
  }

  public Task UpdateDeviceChartA()
  {
    if(ChartConfigA is null)
    {
      ChartA = null;
      return Task.CompletedTask;
    }

    var id = ChartConfigA.GetAssociatedDeviceIds().FirstOrDefault();

    if(id is null)
      return Task.CompletedTask;

    var device = _deviceProvider.GetDevice(id);

    if(device is not null)
      ChartA = new VisualizationItemViewModel(ChartConfigA, [device], OnChartOneDeleteRequested, OnChartOneUpdated);
    
    return Task.CompletedTask;
  }

  public Task UpdateDeviceChartB()
  {
    if(ChartConfigB is null)
    {
      ChartB = null;
      return Task.CompletedTask;
    }

    var id = ChartConfigB.GetAssociatedDeviceIds().FirstOrDefault();

    if(id is null)
      return Task.CompletedTask;

    var device = _deviceProvider.GetDevice(id);

    if(device is not null)
      ChartB = new VisualizationItemViewModel(ChartConfigB, [device], OnChartTwoDeleteRequested, OnChartTwoUpdated);

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

  /// <summary>
  /// When the page is refreshed it wipes the information we have regarding on-going campaigns being executed. 
  /// This method recalls that information so the user picks up where they left off.
  /// </summary>
  /// <returns>A <see cref="Task"/></returns>
  public async Task RefreshExecutionContext()
  {
    await RefreshPlannerTransactionData();
    await RefreshAnalyzerTransactionData();
  }

  public async Task RefreshPlannerTransactionData()
  {
    var plannerTransactions = await _automationClient.GetLatestPlanningTransactions();

    foreach(var transactionList in plannerTransactions)
    {
      if(transactionList is null)
        continue;

      foreach(var (index, item) in transactionList.Index())
      {
        OnPlannerTransactionReceived(item, index);
      }
    }
  }

  public async Task RefreshAnalyzerTransactionData()
  {
    var analyzerTransactions = await _automationClient.GetLatestAnalyzerTransactions();

    foreach(var (index, item) in analyzerTransactions.Index())
    {
      OnAnalyzerTransactionReceived(item, index);
    }
  }

  public async Task RefreshCampaignSetup()
  {
    CurrentStopCondition = await _automationClient.GetActiveStopCondition(new Empty(), null);

    if(CurrentStopCondition.ActiveCondition.Contains("NumExperimentsRun", StringComparison.OrdinalIgnoreCase))
      ActiveStopConditionMode = ExecutionStopConditionMode.NumExperiments;
    else if(CurrentStopCondition.ActiveCondition.Contains("Analysis", StringComparison.OrdinalIgnoreCase))
      ActiveStopConditionMode = ExecutionStopConditionMode.AnalyzerResult;

    var replanRate = await GetCurrentReplicateRate();
    DesiredReplicationRate = replanRate.ReplicateRate_;

    await RefreshExecutionEligibility();
  }

  public IReadOnlyList<ExecutionPreflightItem> PreflightItems
  {
    get
    {
      var templateSelected = CampaignTemplate is not null;
      var stopConditionSet = CurrentStopCondition is not null && CurrentStopCondition.ActiveCondition != "None";
      var plannerRequired = CampaignTemplate?.ExperimentTemplate.GetAllPlannedParameters().Any() == true;
      var analyzerRequired = !string.IsNullOrWhiteSpace(CampaignTemplate?.ExperimentTemplate.AnalyzerId);
      var plannerReady = !plannerRequired || PlannerAdapterInfos.Any();
      var analyzerReady = !analyzerRequired || AnalyzerInfo is not null;

      return
      [
        new("Campaign template", templateSelected, templateSelected ? CampaignTemplate!.Name : "Select a campaign before starting."),
        new("Stop condition", stopConditionSet, stopConditionSet ? CurrentStopCondition!.Description : "Choose how the run should stop."),
        new("Planner", plannerReady, PlannerAdapterInfos.Any() ? string.Join(", ", PlannerAdapterInfos.Select(info => info?.Name).Where(name => !string.IsNullOrWhiteSpace(name))) : "No planner-backed parameters detected."),
        new("Analyzer", analyzerReady, AnalyzerInfo?.Name ?? "No analyzer assigned to the selected experiment."),
        new("Eligibility", LastExecutionEligibility?.IsEligible == true, LastExecutionEligibility?.IsEligible == true ? "Camapign is eligible to start." : LastExecutionEligibility?.Error ?? "Campaign is not eligible to start.")
      ];
    }
  }

  public bool PreflightReady => PreflightItems.All(item => item.IsReady);

  public int CompletedExperimentCount => ExperimentExecutionStatuses.Count(status =>
    status.GetCommandExecutionStatuses().All(command => command.State == ExecutionState.Succeeded));

  public int ActiveExperimentNumber
  {
    get
    {
      var activeIndex = ExperimentExecutionStatuses.ToList().FindIndex(status => status.IsActive());
      return activeIndex >= 0 ? activeIndex + 1 : ExperimentExecutionStatuses.Count;
    }
  }

  public CommandExecutionStatus? CurrentCommand => ExperimentExecutionStatuses
    .LastOrDefault()?
    .GetCommandExecutionStatuses()
    .FirstOrDefault(command => command.State is ExecutionState.Running or ExecutionState.AwaitingUser or ExecutionState.Paused or ExecutionState.WaitingForUserDecision)
    ?? ExperimentExecutionStatuses.LastOrDefault()?.GetCommandExecutionStatuses().FirstOrDefault(command => command.State == ExecutionState.Waiting);

  public string RunStateLabel => CampaignExecutionState?.ToString() ?? "Not Started";

  public string StopConditionSummary => CurrentStopCondition?.Description ?? "No stop condition assigned.";

  public string CampaignSummaryName => CampaignTemplate?.Name ?? SelectedTemplateSummary?.CampaignName ?? "No campaign selected";

  public int CampaignStepCount => CampaignTemplate?.ExperimentTemplate.StepTemplates.Count ?? 0;

  public int CampaignCommandCount => CampaignTemplate?.ExperimentTemplate.StepTemplates.Sum(step => step.CommandTemplates.Count) ?? 0;

  public string PlannerSummary => PlannerAdapterInfos.Any()
    ? string.Join(", ", PlannerAdapterInfos.Select(info => info?.Name).Where(name => !string.IsNullOrWhiteSpace(name)))
    : "No planner";

  public string AnalyzerSummary => AnalyzerInfo?.Name ?? "No analyzer";

  [Reactive]
  public partial ExperimentStopConditionResponse? CurrentStopCondition { get; set; }
  public double DesiredResult { get; set; }
  public double DesiredLeeway { get; set; }
  public int DesiredReplicationRate { get; set; } = 1;

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
  public partial Dictionary<string, List<ChartMetricPoint>> PlannerMetricsMap { get; private set; }
  [Reactive]
  public partial Dictionary<string, List<ChartMetricPoint>> AnalyzerMetrics { get; private set; }
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
  [Reactive]
  public partial int SelectedExecutionTabIndex { get; set; }
  [Reactive]
  public partial ExecutionStopConditionMode ActiveStopConditionMode { get; set; }
  [Reactive]
  public partial CheckExecutionEligibilityResponse? LastExecutionEligibility { get; private set; }
  public uint ExperimentsToRun { get; set; }
  public string ExecutionNotes { get; set; } = string.Empty;
  public CampaignExecutionSummary? TestCampaignExecutionSummary { get; private set; }
  public IEnumerable<CampaignExecutionSummaryMetadata>? TestCampaignResultMetadata { get; private set; }
  public bool DisplayExecutionSummary { get; set; }
  public List<AresCampaignTag> AvailableTags { get; set; } = [];
  public List<AresCampaignTag> SelectedTags { get; set; } = [];
  public string? NewTagName { get; set; }
  [Reactive]
  public partial Dictionary<string, AresValue> CurrentOutputVariables { get; set; } = new();
}

public enum ExecutionStopConditionMode
{
  NumExperiments,
  AnalyzerResult
}

public record ExecutionPreflightItem(string Label, bool IsReady, string Detail);

public class ChartMetricPoint
{
  public int ExecutionIndex { get; set; }
  public double RawValue { get; set; }
  public double PlotValue { get; set; }
}
