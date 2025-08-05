using System.Reactive.Linq;
using System.Reactive.Subjects;
using Ares.Core.Analyzing;
using Ares.Core.AresEnvironment;
using Ares.Core.Execution.ControlTokens;
using Ares.Core.Execution.Executors.Composers;
using Ares.Core.Execution.Extensions;
using Ares.Core.Execution.StopConditions;
using Ares.Core.Notifications;
using Ares.Core.Output;
using Ares.Core.Planning;
using Ares.Messaging;
using Ares.Messaging.Analyzing;
using Google.Protobuf.WellKnownTypes;

namespace Ares.Core.Execution.Executors;

public class CampaignExecutor : ICampaignExecutor
{
  private readonly IExecutionReporter _executionReporter;
  private readonly ISubject<CampaignExecutionStatus> _executionStatusSubject;
  private readonly ICommandComposer<ExperimentTemplate, ExperimentExecutor> _experimentComposer;
  private readonly ICommandComposer<ExperimentTemplate, StartupScriptExecutor> _startupScriptComposer;
  private readonly ICommandComposer<ExperimentTemplate, CloseoutScriptExecutor> _closeoutScriptComposer;
  private readonly IPlanningHelper _planningHelper;
  private readonly IEnumerable<IExecutionSummaryHandler> _summaryHandlers;
  private readonly IEnumerable<INotificationHandler> _notificationHandlers;
  private readonly AresVariableManager _variableManager;
  readonly AnalysisHelper _analysisHelper;
  readonly AnalysisRepo _analysisRepo;
  readonly IAnalyzerRepo _analyzerRepo;

  internal CampaignExecutor(ICommandComposer<ExperimentTemplate, ExperimentExecutor> experimentComposer,
    ICommandComposer<ExperimentTemplate, StartupScriptExecutor> startupScriptComposer,
    ICommandComposer<ExperimentTemplate, CloseoutScriptExecutor> closeoutScriptComposer,
    IPlanningHelper planningHelper,
    IExecutionReporter executionReporter,
    AnalysisHelper analysisHelper,
    CampaignTemplate template,
    IEnumerable<IExecutionSummaryHandler> resultHandlers,
    AnalysisRepo analysisRepo,
    IEnumerable<INotificationHandler> notificationHandlers,
    IAnalyzerRepo analyzerRepo,
    AresVariableManager variableManager)
  {
    _analyzerRepo = analyzerRepo;
    _analysisRepo = analysisRepo;
    _analysisHelper = analysisHelper;
    _variableManager = variableManager;
    _experimentComposer = experimentComposer;
    _startupScriptComposer = startupScriptComposer;
    _closeoutScriptComposer = closeoutScriptComposer;
    _planningHelper = planningHelper;
    _executionReporter = executionReporter;
    _notificationHandlers = notificationHandlers;
    _summaryHandlers = resultHandlers;
    Template = template;

    Status = new CampaignExecutionStatus
    {
      CampaignId = template.UniqueId,
      State = ExecutionState.Waiting
    };

    _executionStatusSubject = new BehaviorSubject<CampaignExecutionStatus>(Status);
    ExperimentStatusObservable = _executionStatusSubject.AsObservable();
  }

  public async Task<CampaignExecutionSummary> Execute(ExecutionControlToken token)
  {
    var startTime = DateTime.UtcNow;

    //Init Campaign Directories
    var campaignPath = await CampaignOutputHelper.InitializeOutputDirectories(Template, startTime);

    if(!string.IsNullOrEmpty(ExecutionNotes))
      await CampaignOutputHelper.WriteExperimentNotes(campaignPath, ExecutionNotes);

    if(CampaignTags.Any())
      await CampaignOutputHelper.WriteExperimentTags(campaignPath, CampaignTags);

    // TODO do something about the analyzers here
    var analyzerId = Template.ExperimentTemplates.First().AnalyzerId;
    if(analyzerId is not null)
    {
      var analyzer = _analyzerRepo.GetAnalyzerById(analyzerId);
      await CampaignOutputHelper.OutputVersionFile(campaignPath, Template, analyzer);
    }

    var experimentSummaries = new List<ExperimentExecutionSummary>();
    var analyses = new List<Analysis>();
    Status = new CampaignExecutionStatus
    {
      CampaignId = Template.UniqueId,
      State = ExecutionState.Waiting
    };

    _analysisRepo.ClearAnalyses();
    Status.State = token.IsPaused ? ExecutionState.Paused : ExecutionState.Running;
    _executionReporter.Report(Status);

    await HandleNotification("Campaign Started!", $"ARES has started a campaign named {Template.Name} successfully!", NotificationSeverityEnum.Success);

    var startupExecutor = GenerateStartupScriptExecutor(token.CancellationToken);
    await HandleExperimentStartup(token, startupExecutor);
    bool executionSuccess = true;
    var experiment_count = 0;

    while(!ShouldStop() && !token.IsCancelled)
    {
      var experimentFolder = $"Experiment_{++experiment_count}";
      var experimentPath = CampaignOutputHelper.CreateExperimentSubFolder(campaignPath, experimentFolder);

      //Populate Internal Variables Related to Experiment
      AresEnvironment.AresEnvironment.SetInternalVariable(InternalVariableType.CurrentExperimentNumber, experiment_count.ToString());

      var experimentExecutorResult = await GenerateExperimentExecutor(analyses, experimentSummaries.Select(es => es.CompletedExperiment), token.CancellationToken);

      if(experimentExecutorResult.ErrorString is not null)
      {
        await HandleNotification("Experiment Executor Generation Failure", experimentExecutorResult.ErrorString, NotificationSeverityEnum.Error);
        executionSuccess = false;
        break;
      }

      if(experimentExecutorResult.ExperimentExecutor is null)
      {
        await HandleNotification("Experiment Executor Generation Failure", "Error was not specified, but the executor generation has failed.", NotificationSeverityEnum.Error);
        executionSuccess = false;
        break;
      }

      var experimentExecutor = experimentExecutorResult.ExperimentExecutor;

      if(experimentExecutorResult.ErrorString is not null)
      {
        await HandleNotification("Experiment Executor Generation Failure", experimentExecutorResult.ErrorString, NotificationSeverityEnum.Error);
        executionSuccess = false;
        break;
      }

      Status.ExperimentExecutionStatuses.Add(experimentExecutor.Status);
      experimentExecutor.ExperimentStatusObservable.Subscribe(experimentStatus =>
      {
        _executionReporter.Report(experimentStatus);

        if(IsAwaitingResponse(experimentStatus))
          Status.State = ExecutionState.AwaitingUser;

        else
          Status.State = token.IsPaused ? ExecutionState.Paused : ExecutionState.Running;
        _executionStatusSubject.OnNext(Status);
        _executionReporter.Report(Status);
      });

      var experimentSummary = await experimentExecutor.Execute(token);
      experimentSummary.ResultOutputPath = experimentPath;

      //If a command failed, stop the experiment.
      if(experimentSummary.StepSummaries.Any(step => step.CommandSummaries.Any(cmd => !cmd.Result.Success)) || !experimentSummary.StepSummaries.Any())
        break;


      // if the execution was canceled, the experiment may not have executed the command to provide the output
      // and thus sending a null result to the analyzer might break it depending on the analyzer
      if(!token.IsCancelled)
      {
        var analysis = await _analysisHelper.Analyze(
          experimentExecutor.Template,
          experimentSummary,
          token.CancellationToken);
        analyses.Add(analysis);

        _analysisRepo.Add(analysis);
        if(analysis.ErrorString is not null)
        {
          await HandleNotification("Analysis Process Failed!", analysis.ErrorString, NotificationSeverityEnum.Error);
          executionSuccess = false;
          break;
        }
      }
      else
      {
        executionSuccess = false;
      }

      await PostExperimentExecution(experimentSummary);
      experimentSummaries.Add(experimentSummary);
    }

    var closeoutExecutor = GenerateCloseoutScriptExecutor(token.CancellationToken);
    await HandleExperimentCloseout(token, closeoutExecutor);

    if(executionSuccess)
    {
      Status.State = ExecutionState.Succeeded;
      await HandleNotification("Campaign Completed", $"ARES completed the {Template.Name} campaign successfully.", NotificationSeverityEnum.Success);
    }

    else
      Status.State = ExecutionState.Failed;

    _executionReporter.Report(Status);

    var campaignExecutionSummary = new CampaignExecutionSummary
    {
      UniqueId = Guid.NewGuid().ToString(),
      CampaignId = Template.UniqueId,
      ExecutionInfo = new ExecutionInfo
      {
        Timezone = TimeZoneInfo.Local.DisplayName,
        LocaltimeOffset = DateTimeOffset.Now.Offset.ToString(),
        TimeFinished = DateTime.UtcNow.ToTimestamp(),
        TimeStarted = startTime.ToUniversalTime().ToTimestamp()
      }
    };

    campaignExecutionSummary.ExperimentSummaries.AddRange(experimentSummaries);
    ExecutionNotes = string.Empty;

    return campaignExecutionSummary;
  }

  private bool ShouldStop()
  {
    return StopConditions.Any(condition => condition.ShouldStop());
  }

  public void UpdateExecutionNotes(string notes) => ExecutionNotes = notes;

  public void UpdateCampaignTags(List<AresCampaignTag> tags) => CampaignTags = tags;

  private bool IsAwaitingResponse(ExperimentExecutionStatus status)
    => status.StepExecutionStatuses
    .Any(step => step.CommandExecutionStatuses
    .Any(cmd => cmd.State == ExecutionState.AwaitingUser));

  private async Task<ExperimentExecutorResult> GenerateExperimentExecutor(IEnumerable<Analysis> analyses, IEnumerable<CompletedExperiment> previousExperiments, CancellationToken cancellationToken)
  {
    var result = new ExperimentExecutorResult();

    // campaign template should have exactly one experiment template at this time
    var experimentTemplate = Template.ExperimentTemplates.First().CloneWithNewIds();
    if(!experimentTemplate.IsResolved())
    {
      if(analyses.Count() % ReplanRate == 0)
      {
        var resolveSuccess = await _planningHelper.TryResolveParameters(Template.PlannerAllocations, experimentTemplate.GetAllPlannedParameters(), analyses, previousExperiments, cancellationToken);
        if(!resolveSuccess)
        {
          result.ErrorString = "Failed to plan! Experiment will be terminated!";
          return result;
        }
      }

      else
        experimentTemplate = previousExperiments.Last().Template.CloneWithNewIds();
    }

    if(!experimentTemplate.IsEnvironmentResolved())
    {
      var resolveVarsSuccess = _variableManager.TryResolveVariable(experimentTemplate.GetAllParameters());

      if(!resolveVarsSuccess)
      {
        result.ErrorString = "Failed to assign environment variables! Experiment will be terminated!";
        return result;
      }
    }

    //Passing the campaigns name into the experiment template for file creation purposes post experiment
    experimentTemplate.Name = Template.Name;

    result.ExperimentExecutor = _experimentComposer.Compose(experimentTemplate);
    return result;
  }

  private StartupScriptExecutor? GenerateStartupScriptExecutor(CancellationToken cancellationToken)
  {
    var experimentTemplate = Template.ExperimentTemplates.First().CloneWithNewIds();

    //Passing the campaigns name into the experiment template for file creation purposes post experiment
    experimentTemplate.Name = Template.Name;

    var resolveVarsSuccess = _variableManager.TryResolveVariable(experimentTemplate.GetAllStartupParameters());

    return _startupScriptComposer.Compose(experimentTemplate);
  }

  private CloseoutScriptExecutor? GenerateCloseoutScriptExecutor(CancellationToken cancellationToken)
  {
    var experimentTemplate = Template.ExperimentTemplates.First().CloneWithNewIds();

    //Passing the campaigns name into the experiment template for file creation purposes post experiment
    experimentTemplate.Name = Template.Name;

    return _closeoutScriptComposer.Compose(experimentTemplate);
  }

  private void RecallPreviousExperiment(IEnumerable<Analysis> analyses, ExperimentTemplate currentTemplate)
  {
    var previousExperiment = analyses.LastOrDefault();
  }

  public async Task HandleExperimentStartup(ExecutionControlToken executionToken, StartupScriptExecutor? startupExecutor)
  {
    if(startupExecutor is null)
      throw new InvalidOperationException("Startup Executor returned null, cannot execute experiment!");

    startupExecutor.ExperimentStatusObservable.Subscribe(startupStatus =>
    {
      _executionReporter.Report(startupStatus);
      Status.State = executionToken.IsPaused ? ExecutionState.Paused : ExecutionState.Running;
      _executionStatusSubject.OnNext(Status);
      _executionReporter.Report(Status);
    });

    await startupExecutor.Execute(executionToken);
  }

  public async Task HandleExperimentCloseout(ExecutionControlToken executionToken, CloseoutScriptExecutor? closeoutExecutor)
  {
    if(closeoutExecutor is null)
      throw new InvalidOperationException("Closeout Executor returned null, cannot execute closeout script!");

    closeoutExecutor.ExperimentStatusObservable.Subscribe(closeoutStatus =>
    {
      _executionReporter.Report(closeoutStatus);
      Status.State = executionToken.IsPaused ? ExecutionState.Paused : ExecutionState.Running;
      _executionStatusSubject.OnNext(Status);
      _executionReporter.Report(Status);
    });

    await closeoutExecutor.Execute(executionToken);
  }

  private async Task PostExperimentExecution(ExperimentExecutionSummary summary)
  {
    foreach(var handler in _summaryHandlers)
    {
      await handler.Handle(summary);
    }
  }

  private async Task HandleNotification(string title, string message, NotificationSeverityEnum severity)
  {
    foreach(var handler in _notificationHandlers)
    {
      await handler.HandleNotification(title, message, severity);
    }
  }

  public CampaignTemplate Template { get; }
  public IList<IStopCondition> StopConditions { get; } = new List<IStopCondition>();
  public double ReplanRate { get; set; } = 1;
  public string? ExecutionNotes { get; set; }
  public List<AresCampaignTag> CampaignTags { get; set; } = new();
  public IObservable<CampaignExecutionStatus> ExperimentStatusObservable { get; }
  public CampaignExecutionStatus Status { get; private set; }
}
