using System.Reactive.Linq;
using System.Reactive.Subjects;
using Ares.Core.Analyzing;
using Ares.Core.AresEnvironment;
using Ares.Core.Device.State.Logging;
using Ares.Core.Exceptions;
using Ares.Core.Execution.ControlTokens;
using Ares.Core.Execution.Executors.Composers;
using Ares.Core.Execution.Extensions;
using Ares.Core.Execution.Safety;
using Ares.Core.Execution.StopConditions;
using Ares.Core.Notifications;
using Ares.Core.Output;
using Ares.Core.Planning;
using Ares.Core.Settings;
using Ares.Datamodel;
using Ares.Datamodel.Analyzing;
using Ares.Datamodel.Templates;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;

namespace Ares.Core.Execution.Executors;

public class CampaignExecutor : ICampaignExecutor
{
  private readonly IExecutionReporter _executionReporter;
  private readonly ISubject<CampaignExecutionStatus> _executionStatusSubject;
  private readonly ICommandComposer<ExperimentTemplate, ExperimentExecutor> _experimentComposer;
  private readonly IPlanningHelper _planningHelper;
  private readonly IEnumerable<IExecutionSummaryHandler> _summaryHandlers;
  private readonly INotifier _notifier;
  private readonly ILogger _logger;
  private readonly AresVariableManager _variableManager;
  private readonly StateLoggerManager _stateLoggerManager;
  readonly AnalysisHelper _analysisHelper;
  readonly AnalysisRepo _analysisRepo;
  readonly IAnalyzerRepo _analyzerRepo;
  readonly ISystemSettingsManager _settingsManager;
  readonly IExecutionSafetyManager _executionSafetyManager;

  internal CampaignExecutor(ICommandComposer<ExperimentTemplate, ExperimentExecutor> experimentComposer,
    IPlanningHelper planningHelper,
    IExecutionReporter executionReporter,
    AnalysisHelper analysisHelper,
    CampaignTemplate template,
    IEnumerable<IExecutionSummaryHandler> resultHandlers,
    AnalysisRepo analysisRepo,
    INotifier notifier,
    IAnalyzerRepo analyzerRepo,
    ILogger<CampaignExecutor> logger,
    AresVariableManager variableManager,
    StateLoggerManager stateLoggerManager,
    ISystemSettingsManager settingsManager,
    IExecutionSafetyManager executionSafetyManager)
  {
    _analyzerRepo = analyzerRepo;
    _analysisRepo = analysisRepo;
    _analysisHelper = analysisHelper;
    _variableManager = variableManager;
    _stateLoggerManager = stateLoggerManager;
    _experimentComposer = experimentComposer;
    _planningHelper = planningHelper;
    _executionReporter = executionReporter;
    _summaryHandlers = resultHandlers;
    _logger = logger;
    Template = template;
    _notifier = notifier;
    _settingsManager = settingsManager;
    _executionSafetyManager = executionSafetyManager;

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
    // Make sure we log all the device state changes during execution.
    await _stateLoggerManager.EnableOverrideAsync(new Datamodel.Device.DeviceLoggingSettings
    {
      LoggingType = Datamodel.Device.DeviceLoggingSettings.Types.LoggingType.OnChange
    });

    var campaignStartTime = DateTime.UtcNow;
    var experimentSummaries = new List<ExperimentExecutionSummary>();
    ExperimentExecutionSummary startupSummary = new();
    ExperimentExecutionSummary closeoutSummary = new();
    
    try
    {
      var campaignPath = await InitializeCampaign(campaignStartTime);
      
      var analyses = new List<Analysis>();
      
      ResetStatus(token);
      await NotifyCampaignStart();

      var executionSuccess = true;


      var startupResult = await ExecuteStartup(analyses, experimentSummaries, token);
      if(!startupResult.Success)
      {
        _logger.LogError("Failed to run the campaign as the startup script failed to execute. {Summary}", startupResult.Summary);
        await _notifier.Notify("Campaign Failure", $"Startup script failed to run. ${startupResult.Summary}", NotificationSeverityEnum.Error);
        return new CampaignExecutionSummary();
      }
      startupSummary = startupResult.Summary;
      

      executionSuccess = await ExecuteExperimentLoop(campaignPath, analyses, experimentSummaries, token, executionSuccess, startupResult.Summary);

      _logger.LogDebug("The campaign loop is now finished. Moving onto the closeout script.");


      var closeoutResult = await ExecuteCloseout(analyses, experimentSummaries, token, executionSuccess);
      closeoutSummary = closeoutResult.Summary;

      await ReportFinalStatus(executionSuccess);
    }
    catch(Exception ex)
    {
      _logger.LogDebug("Exception Caught in Execution! {Exception}", ex.Message);
      _logger.LogDebug("{Trace}", ex.StackTrace);
      await ReportFinalStatus(false);
    }
    finally
    {
      await _stateLoggerManager.DisableOverrideAsync();
    }
    
    return CreateCampaignSummary(campaignStartTime, experimentSummaries, startupSummary, closeoutSummary);
  }

  private async Task<string> InitializeCampaign(DateTime startTime)
  {
    var campaignPath = await CampaignOutputHelper.InitializeOutputDirectories(Template, startTime);
    _logger.LogInformation("Campaign started with output directory at: {Path}", campaignPath);

    if(!string.IsNullOrEmpty(ExecutionNotes))
      await CampaignOutputHelper.WriteExperimentNotes(campaignPath, ExecutionNotes);

    if(CampaignTags.Any())
      await CampaignOutputHelper.WriteExperimentTags(campaignPath, CampaignTags);

    var analyzerId = string.IsNullOrEmpty(Template.ExperimentTemplate.AnalyzerId) ? NoneAnalyzer.Id : Template.ExperimentTemplate.AnalyzerId;
    if (analyzerId is null) 
      return campaignPath;
      
    var analyzer = _analyzerRepo.GetAnalyzerById(analyzerId);
    _logger.LogInformation("Analyzer selected {AnalyzerName}", analyzer?.Name ?? "NO ANALYZER");
    await CampaignOutputHelper.OutputVersionFile(campaignPath, Template, analyzer);

    return campaignPath;
  }

  private void ResetStatus(ExecutionControlToken token)
  {
    Status = new CampaignExecutionStatus
    {
      CampaignId = Template.UniqueId,
      State = ExecutionState.Waiting
    };

    _analysisRepo.ClearAnalyses();
    Status.State = token.IsPaused ? ExecutionState.Paused : ExecutionState.Running;
    _executionReporter.Report(Status);
  }

  private async Task NotifyCampaignStart()
  {
    await _notifier.Notify("Campaign Started!", $"ARES has started a campaign named {Template.Name} successfully!", NotificationSeverityEnum.Success);
    _logger.LogInformation("Campaign started named {CampaignName}", Template.Name);
  }

  private async Task<(bool Success, ExperimentExecutionSummary Summary)> ExecuteStartup(List<Analysis> analyses, List<ExperimentExecutionSummary> experimentSummaries, ExecutionControlToken token)
  {
    _logger.LogDebug("Startup template not null, will run startup script");
    var startupExecutorResult = await GenerateExperimentExecutor(Template.StartupTemplate, analyses, experimentSummaries.Select(es => es.ExperimentOverview), token.CancellationToken);
    if(startupExecutorResult.ErrorString is not null || startupExecutorResult.ExperimentExecutor is null)
    {
      await _notifier.Notify("Campaign Failed!", $"ARES failed to run startup routine for {Template.Name}, campaign will shut down.", NotificationSeverityEnum.Error);
      _logger.LogWarning("Failed to run campaign startup routine for campaign {Name}", Template.Name);
      return (false, new ExperimentExecutionSummary());
    }

    var startupSummary = await ExecuteTemplate(startupExecutorResult.ExperimentExecutor, token);
    startupSummary.ResultOutputPath = AresEnvironment.AresEnvironment.GetEnvironmentVariable(VariableType.CampaignStartupFolder);
    if(Template.StartupTemplate.StepTemplates.Any())
      await PostExperimentExecution(startupSummary);

    _logger.LogDebug("Ran campaign startup template");
    return (true, startupSummary);
  }

  private async Task<bool> ExecuteExperimentLoop(string campaignPath, List<Analysis> analyses, List<ExperimentExecutionSummary> experimentSummaries, ExecutionControlToken token, bool executionSuccess, ExperimentExecutionSummary startupSummary)
  {
    var campaignFailedTitle = "Campaign Execution Failed!";
    var currentState = ExecutionState.InitializeExperiment;
    executionSuccess = true;
    var experimentCount = 0;
    var currentExperimentFolder = "";
    var currentExperimentPath = "";

    ExperimentExecutorResult currentExecutorResult = new();
    ExperimentExecutionSummary? currentSummary = null;
    ExperimentTemplate? currentExperimentTemplate = null;

    while(!ShouldStop() && !token.IsCancelled && executionSuccess != false)
    {
      switch(currentState)
      {
        case ExecutionState.InitializeExperiment:
          experimentCount++;
          currentExperimentFolder = $"Experiment_{++experimentCount}";
          currentExperimentPath = CampaignOutputHelper.CreateExperimentSubFolder(campaignPath, currentExperimentFolder);
          currentExperimentTemplate = Template.ExperimentTemplate.CloneWithNewIds();
          currentExperimentTemplate.Name = Template.Name;

          AresEnvironment.AresEnvironment.SetInternalVariable(InternalVariableType.CurrentExperimentNumber, experimentCount.ToString());
          _logger.LogInformation("ARES is initializing experiment {exp_number}, and has created the associated directories.", experimentCount);
          currentState = ExecutionState.Planning;
          break;

        case ExecutionState.GenerateExecutor:
          if(currentExperimentTemplate is null)
          {
            var nullTemplateMsg = "ARES attempted to generate an experiment executor, but failed due to a null experiment template! The experiment will be terminated.";
            _logger.LogError(nullTemplateMsg);
            await _notifier.Notify(campaignFailedTitle, nullTemplateMsg, NotificationSeverityEnum.Error);
            currentState = ExecutionState.Failed;
            break;
          }

          try
          {
            currentExecutorResult = new ExperimentExecutorResult();
            currentExecutorResult.ExperimentExecutor = _experimentComposer.Compose(currentExperimentTemplate);
            currentState = ExecutionState.Running;
          }

          catch(Exception e)
          {
            currentState = ExecutionState.Failed;
            _logger.LogError("ARES encountered an error while composing an experiment executor! {msg}", e.Message);
            await _notifier.Notify(campaignFailedTitle, $"Executor Composition Error: {e.Message}", NotificationSeverityEnum.Error);
            break;
          }

          break;

        case ExecutionState.Succeeded:
          _logger.LogInformation("ARES successfully executed the experiment {exp_name}!", currentExperimentTemplate?.Name ?? "UNKNOWN");
          break;

        case ExecutionState.Running:
          if(currentExecutorResult is null || currentExecutorResult.ExperimentExecutor is null)
          {
            _logger.LogError("ARES tried to execute an experiment but failed due to a null experiment executor.");
            currentState = ExecutionState.Failed;
            break;
          }

          var experimentSummary = await ExecuteTemplate(currentExecutorResult.ExperimentExecutor, token);
          experimentSummary.ResultOutputPath = currentExperimentPath;

          //Handle Failure Cases Here....

          await PostExperimentExecution(experimentSummary);
          experimentSummaries.Add(experimentSummary);
          break;

        case ExecutionState.Failed:
          executionSuccess = false;
          _logger.LogInformation("ARES experiment reached the 'Failed' execution state. Experiment will be terminated.");
          break;

        case ExecutionState.WaitingForUserDecision:
          break;

        case ExecutionState.Replanning:
          _logger.LogInformation("ARES had an experiment fail, but based on your settings will attempt to re-plan the experiment and run it again.");
          currentState = ExecutionState.Planning;
          break;

        case ExecutionState.Retrying:
          _logger.LogInformation("ARES had an experiment fail, but based on your settings will retry running the experiment.");
          currentState = ExecutionState.Running;
          break;

        case ExecutionState.EnteringSafeMode:
          await _executionSafetyManager.EnterSafeMode();
          currentState = ExecutionState.Failed;
          break;

        case ExecutionState.Planning:
          if(currentExperimentTemplate is not null && !currentExperimentTemplate.IsResolved())
          {
            _logger.LogTrace("Experiment has not been resolved, ARES will now begin the planning process.");
            if(analyses.Count() % ReplanRate == 0)
            {
              Status.PlannerState = PlannerState.PlanningInProgress;
              _executionStatusSubject.OnNext(Status);
              _executionReporter.Report(Status);
              _logger.LogTrace("Analyses count is {count} and replan rate {rate}", analyses.Count(), ReplanRate);

              var metadata = new RequestMetadata
              {
                CampaignId = Template.UniqueId,
                CampaignName = Template.Name,
                ExperimentId = currentExperimentTemplate.UniqueId,
                SystemName = "ARES OS",
                ExperimentStartTime = DateTime.UtcNow.ToUniversalTime().ToTimestamp()
              };

              var resolveSuccess = await _planningHelper.TryResolveParameters(Template.PlannerAllocations,
                metadata,
                currentExperimentTemplate.GetAllPlannedParameters(),
                analyses,
                experimentSummaries.Select(es => es.ExperimentOverview),
                token.CancellationToken);

              if(!resolveSuccess)
              {
                Status.PlannerState = PlannerState.PlanningError;
                _logger.LogDebug("Failed to resolve the experiment template.");
                currentExecutorResult?.ErrorString = "Failed to plan! Experiment will be terminated!";
              }
            }

            else
            {
              currentExperimentTemplate = experimentSummaries.Select(es => es.ExperimentOverview).Last().Template.CloneWithNewIds();
              _logger.LogDebug("Experiment was already resolved, so ARES cloned the template with new UUID's");
            }

            Status.PlannerState = PlannerState.PlanningComplete;
          }

          if(currentExperimentTemplate is not null && !currentExperimentTemplate.IsEnvironmentResolved())
          {
            _logger.LogTrace("Environment variables were not resolved yet. ARES will attempt to resolve the experiments environment variables.");
            var resolveVarsSuccess = _variableManager.TryResolveVariable(currentExperimentTemplate.GetAllParameters());

            if(!resolveVarsSuccess)
            {
              _logger.LogDebug("Failed to assign environment variables, experiment will be terminated!");
              currentState = ExecutionState.Failed;
              break;
            }

            else
              _logger.LogDebug("Successfully resolved ARES environment variables.");
          }

          currentState = ExecutionState.GenerateExecutor;
          break;

        case ExecutionState.Analyzing:
          if(!token.IsCancelled && currentExecutorResult is not null && currentExecutorResult.ExperimentExecutor is not null && currentSummary is not null)
          {
            var result = await AnalyzeResult(currentExecutorResult.ExperimentExecutor, currentSummary, startupSummary, analyses, token);
            
            if(!result.Success) 
              executionSuccess = false;
            
            if(!result.Continue) 
              break;
          }
          else
            executionSuccess = false;

          break;

        default:
          break;
      }
    }

    return executionSuccess;
  }

  private async Task<bool> ExecuteExperimentLoopArchive(string campaignPath, List<Analysis> analyses, List<ExperimentExecutionSummary> experimentSummaries, ExecutionControlToken token, bool executionSuccess, ExperimentExecutionSummary startupSummary)
  {
    var experimentCount = 0;
    while(!ShouldStop() && !token.IsCancelled && executionSuccess == true)
    {
      var experimentFolder = $"Experiment_{++experimentCount}";
      var experimentPath = CampaignOutputHelper.CreateExperimentSubFolder(campaignPath, experimentFolder);

      //Populate Internal Variables Related to Experiment
      AresEnvironment.AresEnvironment.SetInternalVariable(InternalVariableType.CurrentExperimentNumber, experimentCount.ToString());
      _logger.LogDebug("Set ARES environment variable {VarName} to {VarValue}", InternalVariableType.CurrentExperimentNumber, experimentCount);
      var experimentExecutorResult = await GenerateExperimentExecutor(Template.ExperimentTemplate, analyses, experimentSummaries.Select(es => es.ExperimentOverview), token.CancellationToken);

      if(experimentExecutorResult.ErrorString is not null)
      {
        _logger.LogError("Experiment Executor Generation Failed! Reason: {errorString}", experimentExecutorResult.ErrorString);
        await _notifier.Notify("Experiment Executor Generation Failure", experimentExecutorResult.ErrorString, NotificationSeverityEnum.Error);
        executionSuccess = false;
        break;
      }

      if(experimentExecutorResult.ExperimentExecutor is null)
      {
        _logger.LogError("Experiment Executor Generation Failed! Reason was not specified.");
        await _notifier.Notify("Experiment Executor Generation Failure", "Error was not specified, but the executor generation has failed.", NotificationSeverityEnum.Error);
        executionSuccess = false;
        break;
      }

      var experimentExecutor = experimentExecutorResult.ExperimentExecutor;
      var experimentSummary = await ExecuteTemplate(experimentExecutor, token);
      experimentSummary.ResultOutputPath = experimentPath;

      //COMMAND FAILURE. Check for failure status code, act as needed.
      if(experimentSummary.StepSummaries.Any(step => step.CommandSummaries.Any(cmd => !cmd.Result.Success)) || !experimentSummary.StepSummaries.Any())
      {
        _logger.LogWarning("Command failure detected.");

        executionSuccess = false;
        experimentSummaries.Add(experimentSummary);
        break;
      }

      // if the execution was canceled, the experiment may not have executed the command to provide the output
      // and thus sending a null result to the analyzer might break it depending on the analyzer
      if(!token.IsCancelled)
      {
        var result = await AnalyzeResult(experimentExecutor, experimentSummary, startupSummary, analyses, token);
        if (!result.Success) executionSuccess = false;
        if (!result.Continue) break;
      }
      else
        executionSuccess = false;

      await PostExperimentExecution(experimentSummary);
      experimentSummaries.Add(experimentSummary);
    }
    return executionSuccess;
  }

  private async Task<(bool Success, bool Continue)> AnalyzeResult(ExperimentExecutor experimentExecutor, ExperimentExecutionSummary experimentSummary, ExperimentExecutionSummary startupSummary, List<Analysis> analyses, ExecutionControlToken token)
  {
    var metadata = new RequestMetadata 
    { 
      CampaignId = Template.UniqueId, 
      CampaignName = Template.Name, 
      ExperimentId = experimentExecutor.Template.UniqueId, 
      SystemName = "ARES OS",
      ExperimentStartTime = experimentSummary.ExecutionInfo.TimeStarted
    };

    Status.AnalysisState = AnalysisState.AnalysisInProgress;
    _executionStatusSubject.OnNext(Status);
    _executionReporter.Report(Status);
    var analysis = await _analysisHelper.Analyze(
      experimentExecutor.Template,
      experimentSummary,
      startupSummary,
      metadata,
      token.CancellationToken);

    // The following are top level checks for analysis failure in case the
    // failure is not properly handled on the Analysis itself
    // which also has support for "success" and "error" message
    if (analysis is null || analysis.Result == float.NaN)
    {
      Status.AnalysisState = AnalysisState.AnalysisError;
      await _notifier.Notify("Analysis Failure", $"Analysis was reported as successful, but no actual analysis was provided. {analysis?.ErrorString ?? "No error string provided"}", NotificationSeverityEnum.Error);
      _logger.LogError("Failed to analyze. The analysis result came back as {Result}", analysis?.Result);
      return (false, false);
    }

    if(analysis.AnalysisOutcome == Outcome.Failure)
    {
      Status.AnalysisState = AnalysisState.AnalysisError;
      await _notifier.Notify("Analysis Failure", $"Failed to analyze experiment result: {analysis.ErrorString}", NotificationSeverityEnum.Error);
      _logger.LogError("Failed to analyze. Reason {Error}", analysis.ErrorString);
      return (false, false);         
    }

    else if(analysis.AnalysisOutcome == Outcome.Canceled)
    {
      Status.AnalysisState = AnalysisState.AnalysisError;
      await _notifier.Notify("Analysis Canceled", "The requested analysis has been canceled", NotificationSeverityEnum.Info);
      _logger.LogError("Failed to analyze. Reason: Analysis was canceled");
      return (true, false);
    }

    else if(analysis.AnalysisOutcome == Outcome.Warning) 
    { 
      await _notifier.Notify("Warning From Analyzer!", $"Analysis completed successfully, but the analyzer emitted a warning! {analysis.ErrorString}", NotificationSeverityEnum.Warning);
      _logger.LogWarning("Analysis completed successfully, but the analyzer emitted a warning! {Warning}", analysis.ErrorString);
    }

    Status.AnalysisState = AnalysisState.AnalysisComplete;
    _executionStatusSubject.OnNext(Status);
    _executionReporter.Report(Status);
    analyses.Add(analysis);

    _analysisRepo.Add(analysis);
    // Here the analysis has failed, but the analyzer properly reported why
    // it failed via the analysis result.
    if(analysis.ErrorString != string.Empty && string.IsNullOrEmpty(analysis.ErrorString))
    {
      await _notifier.Notify("Analysis Process Failed!", analysis.ErrorString, NotificationSeverityEnum.Error);
      return (false, false);
    }

    return (true, true);
  }

  private async Task<(bool Success, ExperimentExecutionSummary Summary)> ExecuteCloseout(List<Analysis> analyses, List<ExperimentExecutionSummary> experimentSummaries, ExecutionControlToken token, bool executionSuccess)
  {
    _logger.LogInformation("Closeout template is set. Running the closeout script.");
    var closeoutExecutorResult = await GenerateExperimentExecutor(Template.CloseoutTemplate, analyses, experimentSummaries.Select(es => es.ExperimentOverview), token.CancellationToken);
    if(closeoutExecutorResult?.ErrorString is not null || closeoutExecutorResult?.ExperimentExecutor is null)
    {
      await _notifier.Notify("Closeout Script Failed!", closeoutExecutorResult?.ErrorString ?? "Unknown Closeout Script Failure", NotificationSeverityEnum.Error);
      _logger.LogError("Closeout script failed: {Error}", closeoutExecutorResult?.ErrorString ?? "Unknown error");
      throw new CloseoutScriptFailedException(closeoutExecutorResult?.ErrorString ?? "Closeout failed, but no reason for failure was provided.");
    }

    var closeoutSummary = await ExecuteTemplate(closeoutExecutorResult.ExperimentExecutor, token);
    closeoutSummary.ResultOutputPath = AresEnvironment.AresEnvironment.GetEnvironmentVariable(VariableType.CampaignMiscFolder);
    if(Template.CloseoutTemplate.StepTemplates.Any())
      await PostExperimentExecution(closeoutSummary);
    _logger.LogDebug("Closeout template finished running");
    return (executionSuccess, closeoutSummary);
  }

  private async Task ReportFinalStatus(bool executionSuccess)
  {
    if(executionSuccess)
    {
      Status.State = ExecutionState.Succeeded;
      _logger.LogInformation("Campaign {Name} has finished successfully.", Template.Name);
      await _notifier.Notify("Campaign Completed", $"ARES completed the {Template.Name} campaign successfully.", NotificationSeverityEnum.Success);
    }

    else
    {
      Status.State = ExecutionState.Failed;
      _logger.LogWarning("Campaign {Name} has failed to execute properly.", Template.Name);
      await _notifier.Notify("Campaign Failed", $"ARES failed to execute {Template.Name} successfully, check the event history page for errors.", NotificationSeverityEnum.Error);
    }
      

    _executionReporter.Report(Status);
  }

  private CampaignExecutionSummary CreateCampaignSummary(DateTime startTime, List<ExperimentExecutionSummary> experimentSummaries, ExperimentExecutionSummary? startupSummary, ExperimentExecutionSummary? closeoutSummary)
  {
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
    _logger.LogDebug("Created the campaign execution summary");
    campaignExecutionSummary.ExperimentSummaries.AddRange(experimentSummaries);
    campaignExecutionSummary.StartupExecutionSummary = startupSummary;
    campaignExecutionSummary.CloseoutExecutionSummary = closeoutSummary;
    ExecutionNotes = string.Empty;
    _logger.LogDebug("Finished the campaign execution function");
    return campaignExecutionSummary;
  }

  private bool ShouldStop()
  {
    return StopConditions.Any(condition => condition.ShouldStop());
  }

  public void UpdateExecutionNotes(string notes) => ExecutionNotes = notes;

  public void UpdateCampaignTags(List<AresCampaignTag> tags) => CampaignTags = tags;

  private static bool IsAwaitingResponse(ExperimentExecutionStatus status)
    => status.StepExecutionStatuses
    .Any(step => step.CommandExecutionStatuses
    .Any(cmd => cmd.State == ExecutionState.AwaitingUser));

  private async Task<ExperimentExecutorResult> GenerateExperimentExecutor(ExperimentTemplate template, IEnumerable<Analysis> analyses, IEnumerable<ExperimentOverview> previousExperiments, CancellationToken cancellationToken)
  {
    var result = new ExperimentExecutorResult();
    var experimentTemplate = template.CloneWithNewIds();

    _logger.LogDebug($"Going to try and generate an experiment executor for {template.Name}.");
    if(!experimentTemplate.IsResolved())
    {
      _logger.LogTrace("Experiment was not resolved");
      if(analyses.Count() % ReplanRate == 0)
      {
        Status.PlannerState = PlannerState.PlanningInProgress;
        _executionStatusSubject.OnNext(Status);
        _executionReporter.Report(Status);
        _logger.LogTrace("Analyses count is {count} and replan rate {rate}", analyses.Count(), ReplanRate);
        
        var metadata = new RequestMetadata 
        { 
          CampaignId = Template.UniqueId, 
          CampaignName = Template.Name, 
          ExperimentId = experimentTemplate.UniqueId, 
          SystemName = "ARES OS",
          ExperimentStartTime = DateTime.UtcNow.ToUniversalTime().ToTimestamp()
        };

        var resolveSuccess = await _planningHelper.TryResolveParameters(Template.PlannerAllocations, 
          metadata, 
          experimentTemplate.GetAllPlannedParameters(), 
          analyses, 
          previousExperiments, 
          cancellationToken);

        if(!resolveSuccess)
        {
          Status.PlannerState = PlannerState.PlanningError;
          _logger.LogDebug("Failed to resolve the experiment template.");
          result.ErrorString = "Failed to plan! Experiment will be terminated!";
          return result;
        }
      }

      else
      {
        experimentTemplate = previousExperiments.Last().Template.CloneWithNewIds();

        _logger.LogDebug("Experiment was cloned with new UUID's");
      }
      Status.PlannerState = PlannerState.PlanningComplete;
    }

    if(!experimentTemplate.IsEnvironmentResolved())
    {
      _logger.LogTrace("Environment not resolved yet. Trying to resolve");
      var resolveVarsSuccess = _variableManager.TryResolveVariable(experimentTemplate.GetAllParameters());

      if(!resolveVarsSuccess)
      {
        _logger.LogDebug("Failed to assign environment variables");
        result.ErrorString = "Failed to assign environment variables! Experiment will be terminated!";
        return result;
      }
      else
      {
        _logger.LogDebug("Successfully resolved ARES environment variables.");
      }
    }

    //Passing the campaigns name into the experiment template for file creation purposes post experiment
    experimentTemplate.Name = Template.Name;

    result.ExperimentExecutor = _experimentComposer.Compose(experimentTemplate);
    _logger.LogTrace("Composed experiment template, {TemplateName}", experimentTemplate.Name);

    return result;
  }

  private async Task<ExperimentExecutionSummary> ExecuteTemplate(ExperimentExecutor experimentExecutor, ExecutionControlToken token)
  {
    Status.ExperimentExecutionStatuses.Add(experimentExecutor.Status);
    experimentExecutor.ExperimentStatusObservable.Subscribe(experimentStatus =>
    {
      _executionReporter.Report(experimentStatus);

      if(IsAwaitingResponse(experimentStatus))
        Status.State = ExecutionState.AwaitingUser;

      else
        Status.State = token.IsPaused ? ExecutionState.Paused : ExecutionState.Running;

      _logger.LogDebug("Current experiment status for {ExpName}: {Status}", experimentExecutor.Template.Name, Status.State);
      _executionStatusSubject.OnNext(Status);
      _executionReporter.Report(Status);
    });

    _logger.LogDebug("About to execute the template {TemplateName}", experimentExecutor.Template.Name);
    var result = await experimentExecutor.Execute(token);
    return result;
  }

  private async Task PostExperimentExecution(ExperimentExecutionSummary summary)
  {
    _logger.LogDebug("About to run post experiment execution routine for {TemplateName}", summary.ExperimentOverview.Template.Name);
    foreach(var handler in _summaryHandlers)
    {
      await handler.Handle(summary);
    }
  }

  public CampaignTemplate Template { get; }
  public IList<IStopCondition> StopConditions { get; } = [];
  public double ReplanRate { get; set; } = 1;
  public string? ExecutionNotes { get; set; }
  public List<AresCampaignTag> CampaignTags { get; set; } = [];
  public IObservable<CampaignExecutionStatus> ExperimentStatusObservable { get; }
  public CampaignExecutionStatus Status { get; private set; }
}


