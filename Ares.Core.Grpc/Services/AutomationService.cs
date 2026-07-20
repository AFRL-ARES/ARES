using System.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Ares.Core.Analyzing;
using Ares.Core.Campaigns;
using Ares.Core.Execution;
using Ares.Core.Execution.StartConditions;
using Ares.Core.Execution.StopConditions;
using Ares.Core.Notifications;
using Ares.Datamodel;
using Ares.Datamodel.Templates;
using Ares.Services;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Ares.Datamodel.Planning;
using Ares.Core.Execution.Extensions;
using Ares.Core.Planning;
using Ares.Datamodel.Analyzing;
using Ares.Datamodel.Extensions;
using Ares.Core.Execution.StopConditions.PlannerLead;
using System.Text.Json;

namespace Ares.Core.Grpc.Services;

public class AutomationService : AresAutomation.AresAutomationBase
{
  private readonly IActiveCampaignTemplateStore _activeCampaignTemplateStore;
  private readonly IDbContextFactory<CoreDatabaseContext> _coreContextFactory;
  private readonly IExecutionManager _executionManager;
  private readonly IExecutionReportStore _executionReportStore;
  private readonly IEnumerable<IStartCondition> _startConditions;
  private readonly IEnumerable<INotificationHandler> _notificationHandlers;
  readonly IDesiredAnalysisResultFactory _desiredAnalysisResultFactory;
  private readonly IPlannerLeadStopConditionFactory _plannerLeadStopConditionFactory;
  private readonly IPlannerServiceRepo _plannerServiceRepo;
  private readonly IPlannerTransactionProvider _plannerTransactionProvider;
  private readonly IAnalyzerTransactionProvider _analyzerTransactionProvider;
  private readonly ICampaignTemplatePersistenceService _campaignTemplatePersistenceService;
  private readonly ICampaignTemplateTransferService _campaignTemplateTransferService;

  public AutomationService(IDbContextFactory<CoreDatabaseContext> coreContextFactory,
    IExecutionManager executionManager,
    IExecutionReportStore executionReportStore,
    IActiveCampaignTemplateStore activeCampaignTemplateStore,
    IEnumerable<IStartCondition> startConditions,
    IEnumerable<INotificationHandler> notificationHandlers,
    IDesiredAnalysisResultFactory desiredAnalysisResultFactory,
    IPlannerLeadStopConditionFactory plannerLeadStopConditionFactory,
    IPlannerServiceRepo plannerServiceRepo,
    IPlannerTransactionProvider plannerTransactionProvider,
    IAnalyzerTransactionProvider analyzerTransactionProvider,
    ICampaignTemplatePersistenceService campaignTemplatePersistenceService,
    ICampaignTemplateTransferService campaignTemplateTransferService)
  {
    _desiredAnalysisResultFactory = desiredAnalysisResultFactory;
    _plannerLeadStopConditionFactory = plannerLeadStopConditionFactory;
    _coreContextFactory = coreContextFactory;
    _executionManager = executionManager;
    _executionReportStore = executionReportStore;
    _activeCampaignTemplateStore = activeCampaignTemplateStore;
    _startConditions = startConditions;
    _notificationHandlers = notificationHandlers;
    _plannerServiceRepo = plannerServiceRepo;
    _plannerTransactionProvider = plannerTransactionProvider;
    _analyzerTransactionProvider = analyzerTransactionProvider;
    _campaignTemplatePersistenceService = campaignTemplatePersistenceService;
    _campaignTemplateTransferService = campaignTemplateTransferService;
  }

  public override async Task<ProjectsResponse> GetAllProjects(Empty request, ServerCallContext? context)
  {
    await using var dbContext = await _coreContextFactory.CreateDbContextAsync();
    var projects = await dbContext.Projects.AsNoTracking().ToArrayAsync((context?.CancellationToken ?? CancellationToken.None));
    var response = new ProjectsResponse();
    response.Projects.AddRange(projects);
    return response;
  }

  public override async Task<GetAllCampaignsResponse> GetAllCampaigns(GetAllCampaignsRequest request, ServerCallContext? context)
  {
    var campaignResponse = new GetAllCampaignsResponse();
    var campaigns = await _campaignTemplatePersistenceService.GetSummariesAsync(context?.CancellationToken ?? CancellationToken.None);
    campaignResponse.Campaigns.AddRange(campaigns);

    return campaignResponse;
  }

  public override async Task<BoolValue> CampaignExists(CampaignRequest request, ServerCallContext? context)
  {
    var cancellationToken = context?.CancellationToken ?? CancellationToken.None;
    if(request.HasUniqueId)
      return new BoolValue { Value = await _campaignTemplatePersistenceService.ExistsByIdAsync(request.UniqueId, cancellationToken) };

    else
      return new BoolValue { Value = await _campaignTemplatePersistenceService.ExistsByNameAsync(request.CampaignName, cancellationToken) };
  }

  public override Task<CampaignTemplate?> GetSingleCampaign(CampaignRequest request, ServerCallContext? context)
  => GetCampaignTemplate(request, context);

  public override async Task<Empty> RemoveCampaign(CampaignRequest request, ServerCallContext? context)
  {
    if(_activeCampaignTemplateStore.CampaignTemplate?.UniqueId == request.UniqueId)
    {
      HandleNotification("Cannot Delete Active Campaign", $"ARES rejected a request to delete the campaign {_activeCampaignTemplateStore.CampaignTemplate.Name} as it is currently set as the active campaign.", NotificationSeverityEnum.Info);
      return new Empty();
    }

    await _campaignTemplatePersistenceService.DeleteAsync(request.UniqueId, context?.CancellationToken ?? CancellationToken.None);
    return new Empty();
  }

  public override async Task<Project> GetProject(ProjectRequest request, ServerCallContext? context)
  {
    await using var dbContext = _coreContextFactory.CreateDbContext();
    return await dbContext.Projects.AsNoTracking().FirstAsync(project => project.Name == request.ProjectName, (context?.CancellationToken ?? CancellationToken.None));
  }

  public override async Task<Empty> RemoveProject(ProjectRequest request, ServerCallContext? context)
  {
    await using var dbContext = _coreContextFactory.CreateDbContext();
    var project = await dbContext.Projects.FirstAsync(p => p.Name == request.ProjectName, (context?.CancellationToken ?? CancellationToken.None));
    dbContext.Projects.Remove(project);
    await dbContext.SaveChangesAsync((context?.CancellationToken ?? CancellationToken.None));

    return new Empty();
  }

  public override async Task<Empty> AddProject(Project request, ServerCallContext? context)
  {
    using var dbContext = _coreContextFactory.CreateDbContext();
    dbContext.Projects.Add(request);
    await dbContext.SaveChangesAsync((context?.CancellationToken ?? CancellationToken.None));
    return new Empty();
  }

  /// <summary>
  /// </summary>
  /// <param name="request">
  ///   <see
  /// </param>
  /// <param name="context"></param>
  /// <returns></returns>
  public override async Task<Empty> AddCampaign(AddOrUpdateCampaignRequest request, ServerCallContext? context)
  {
    await _campaignTemplatePersistenceService.AddAsync(request.Template, context?.CancellationToken ?? CancellationToken.None);
    return new Empty();
  }

  public override async Task<CampaignTemplate> UpdateCampaign(AddOrUpdateCampaignRequest request, ServerCallContext? context)
  {
    var updated = await _campaignTemplatePersistenceService.ReplaceAsync(request.Template, context?.CancellationToken ?? CancellationToken.None);
    if(!updated)
    {
      var title = "Error Updating Campaign";
      var message = $"Attempted to update a campaign that didn't exist. {request.Template.Name} couldn't be found in your list of available campaign templates.";
      HandleNotification(title, message, NotificationSeverityEnum.Error);
    }

    return request.Template;
  }

  private async Task<CampaignTemplate?> GetCampaignTemplate(CampaignRequest request, ServerCallContext? context)
  {
    var campaign = await _campaignTemplatePersistenceService.GetByIdAsync(request.UniqueId, context?.CancellationToken ?? CancellationToken.None);
    if(campaign is not null)
      return campaign;

    var title = "Error Fetching Campaign Template";
    var message = $"Attempted to fetch a campaign that didn't exist. {request.CampaignName}'s UUID did not match any campaign in the database. If you deleted a campaign this is expected.";
    HandleNotification(title, message, NotificationSeverityEnum.Warning);
    return null;
  }

  public override Task<CampaignResponse> GetCurrentlySelectedCampaign(Empty request, ServerCallContext? context)
  {
    var template = _activeCampaignTemplateStore.CampaignTemplate;
    var response = new CampaignResponse { HasValue = template is not null, Value = template };
    return Task.FromResult(response);
  }

  public override Task<Empty> StartExecution(StartCampaignRequest request, ServerCallContext? context)
  {
    _executionManager.Start(request.UserNotes, request.CampaignTags.ToList());
    return Task.FromResult(new Empty());
  }

  public override async Task<CampaignTemplate> SetCampaignForExecution(CampaignRequest request, ServerCallContext? context)
  {
    var template = await GetCampaignTemplate(request, context);
    if(template is null)
    {
      throw new InvalidOperationException($"No campaign template found for request. Name: {request.CampaignName}");
    }
    _activeCampaignTemplateStore.CampaignTemplate = template;
    return template;
  }

  public override Task GetExecutionStatusStream(Empty request, IServerStreamWriter<ExperimentExecutionStatus> responseStream, ServerCallContext? context)
  {
    var observable = _executionReportStore.ExperimentStatusObservable;
    return observable.Where(status => status is not null).Do(status => responseStream.WriteAsync(status!)).ToTask((context?.CancellationToken ?? CancellationToken.None));
  }

  public override Task<CampaignExecutionStatusResponse> GetCampaignExecutionStatus(Empty request, ServerCallContext? context)
  {
    var status = _executionReportStore.CampaignExecutionStatus;
    return Task.FromResult(new CampaignExecutionStatusResponse
    {
      Status = status
    });
  }

  public override Task GetCampaignExecutionStateStream(Empty request, IServerStreamWriter<CampaignExecutionState> responseStream, ServerCallContext? context)
  {
    var observable = _executionReportStore.CampaignStatusObservable;
    return observable!
      .OfType<CampaignExecutionStatus>()
      .Select(status => new CampaignExecutionState 
      { 
        CampaignId = status.CampaignId, 
        State = status.State, 
        AnalysisState = status.AnalysisState, 
        PlannerState = status.PlannerState 
      })
      .Do(state => responseStream.WriteAsync(state))
      .ToTask((context?.CancellationToken ?? CancellationToken.None));
  }

  public override Task<Empty> StopExecution(Empty request, ServerCallContext? context)
  {
    _executionManager.Stop();
    return Task.FromResult(new Empty());
  }

  public override Task<Empty> PauseExecution(Empty request, ServerCallContext? context)
  {
    _executionManager.Pause();
    return Task.FromResult(new Empty());
  }

  public override Task<Empty> ResumeExecution(Empty request, ServerCallContext? context)
  {
    _executionManager.Resume();
    return Task.FromResult(new Empty());
  }

  public override Task<Empty> SubmitUserDecision(UserDecisionRequest request, ServerCallContext? context)
  {
    _executionManager.SubmitUserDecision(request.Decision);
    return Task.FromResult(new Empty());
  }

  public override Task<StartStopConditionsResponse> GetAssignedStopConditions(Empty request, ServerCallContext? context)
  {
    var conditions = _executionManager.CampaignStopConditions;
    var response = new StartStopConditionsResponse();
    var startStopConditions = conditions?.Select(condition => new StartStopCondition { Message = condition.Message, Name = condition.GetType().Name }) ?? new List<StartStopCondition>();
    response.StartStopConditions.AddRange(startStopConditions);

    return Task.FromResult(response);
  }

  public override async Task<StartStopConditionsResponse> GetFailedStartConditions(Empty request, ServerCallContext? context)
  {
    var response = new StartStopConditionsResponse();
    var conditionResults = await Task.WhenAll(_startConditions.Select(condition => condition.CanStart()));
    var conditions = conditionResults.Where(result => result is not null && !result.Success).Select(condition => new StartStopCondition { Message = string.Join(Environment.NewLine, condition!.Messages), Name = condition.GetType().Name });
    response.StartStopConditions.AddRange(conditions);

    return response;
  }

  public override Task<Empty> RemoveStopCondition(StartStopCondition request, ServerCallContext? context)
  {
    var stopConditions = _executionManager.CampaignStopConditions;
    if(stopConditions is null)
      return Task.FromResult(new Empty());

    var condition = stopConditions.FirstOrDefault(condition => condition.GetType().Name.Equals(request.Name));
    if(condition is not null)
      stopConditions.Remove(condition);

    return Task.FromResult(new Empty());
  }

  public override async Task<StartStopConditionsResponse> GetPreliminaryFailedStartConditions(CampaignTemplate request, ServerCallContext? context)
  {
    var response = new StartStopConditionsResponse();
    var conditionResults = await Task.WhenAll(_startConditions.Select(condition => condition.CanStart()));
    var conditions = conditionResults.Where(result => result is not null && !result.Success).Select(condition => new StartStopCondition { Message = string.Join(Environment.NewLine, condition!.Messages), Name = condition.GetType().Name });
    response.StartStopConditions.AddRange(conditions);

    return response;
  }

  private async Task<IEnumerable<StartConditionResult>> GetFailedStartConditionResults()
  {
    var conditionTasks = _startConditions.Select(condition => condition.CanStart());
    var conditions = await Task.WhenAll(conditionTasks);
    return conditions;
  }

  public override Task<Empty> SetNumExperimentsStopCondition(NumExperimentsCondition request, ServerCallContext? context)
  {
    var stopConditions = _executionManager.CampaignStopConditions;
    if(stopConditions is null)
      return Task.FromResult(new Empty());

    stopConditions.Clear();

    var newCondition = new NumExperimentsRun(_executionReportStore, request.NumExperiments);
    stopConditions.Add(newCondition);

    return Task.FromResult(new Empty());
  }

  public override Task<Empty> SetReplicateRate(ReplicateRate request, ServerCallContext? context)
  {
    _executionManager.UpdateReplicateRate(request.ReplicateRate_);
    return Task.FromResult(new Empty());
  }

  public override Task<ReplicateRate> GetReplicateRate(Empty request, ServerCallContext? context)
  {
    return Task.FromResult(new ReplicateRate { ReplicateRate_ = _executionManager.ReplicateRate });
  }

  public override Task<Empty> SetPlanningBatchSize(PlanningBatchSize request, ServerCallContext? context)
  {
    _executionManager.UpdateBatchPlanningSize(request.BatchSize);
    return Task.FromResult(new Empty());
  }

  public override Task<PlanningBatchSize> GetPlanningBatchSize(Empty request, ServerCallContext? context)
  {
    return Task.FromResult(new PlanningBatchSize { BatchSize = _executionManager.PlanningBatchSize });
  }

  public override Task<Empty> SetAnalysisResultStopCondition(AnalysisResultCondition request, ServerCallContext? context)
  {
    var stopConditions = _executionManager.CampaignStopConditions;
    if(stopConditions is null)
      return Task.FromResult(new Empty());

    stopConditions.Clear();

    var stopCondition = _desiredAnalysisResultFactory.Create(request.DesiredResult, request.Leeway);
    stopConditions.Add(stopCondition);

    return Task.FromResult(new Empty());
  }

  public override Task<Empty> SetPlannerLeadStopCondition(Empty request, ServerCallContext context)
  {
    var stopConditions = _executionManager.CampaignStopConditions;

    if(stopConditions is null)
      return Task.FromResult(new Empty());

    stopConditions.Clear();

    var stopCondition = _plannerLeadStopConditionFactory.Create();
    stopConditions.Add(stopCondition);

    return Task.FromResult(new Empty());
  }

  public override Task<ExperimentStopConditionResponse> GetActiveStopCondition(Empty request, ServerCallContext? context)
  {
    var stopConditions = _executionManager.CampaignStopConditions;
    if(stopConditions is null || !stopConditions.Any())
    {
      return Task.FromResult(
        new ExperimentStopConditionResponse
        {
          ActiveCondition = "None",
          Description = "No stop conditions assigned."
        });
    }

    var condition = stopConditions.First();
    return Task.FromResult(
      new ExperimentStopConditionResponse
      {
        ActiveCondition = condition.GetType().Name,
        Description = condition.Description
      });
  }

  public override async Task<CheckExecutionEligibilityResponse> CheckExecutionEligibility(Empty request, ServerCallContext? context)
  {
    var eligbilityError = await _executionManager.CheckCampaignStartPrerequisites();

    if(string.IsNullOrEmpty(eligbilityError))
      return new CheckExecutionEligibilityResponse { Error = string.Empty, IsEligible = true };

    else
      return new CheckExecutionEligibilityResponse { Error = eligbilityError, IsEligible = false };
  }

  public override async Task<TagsResponse> GetAllTags(Empty request, ServerCallContext? context)
  {
    await using var dbContext = await _coreContextFactory.CreateDbContextAsync();
    var existingTags = await dbContext.CampaignTags.ToArrayAsync();
    var response = new TagsResponse();
    response.AvailableTags.AddRange(existingTags);
    return response;
  }

  public override async Task<TagsResponse> AddTag(TagRequest request, ServerCallContext? context)
  {
    await using var dbContext = await _coreContextFactory.CreateDbContextAsync();
    var existingTags = await dbContext.CampaignTags.ToArrayAsync();
    if(existingTags.Any(t => t.UniqueId == request.Tag.UniqueId))
      //Duplicate tag, don't do it plz
      throw new InvalidOperationException();

    dbContext.CampaignTags.Add(request.Tag);
    await dbContext.SaveChangesAsync();

    var response = new TagsResponse();
    response.AvailableTags.AddRange(existingTags);
    response.AvailableTags.Add(request.Tag);
    return response;
  }

  public override async Task<TagsResponse> RemoveTag(TagRequest request, ServerCallContext? context)
  {
    await using var dbContext = await _coreContextFactory.CreateDbContextAsync();
    var existingTags = await dbContext.CampaignTags.ToArrayAsync();
    var match = existingTags.FirstOrDefault(tag => tag.UniqueId == request.Tag.UniqueId);

    if(match is not null)
    {
      dbContext.Remove(match);
      await dbContext.SaveChangesAsync();
    }

    var response = new TagsResponse();
    response.AvailableTags.AddRange(await dbContext.CampaignTags.ToArrayAsync());
    return response;
  }

  public override async Task<AvailableCampaignExecutionSummariesResponse> GetAvailableCampaignExecutionSummaries(Empty request, ServerCallContext? context)
  {
    await using var dbContext = await _coreContextFactory.CreateDbContextAsync();
    var summaries = await dbContext.CampaignExecutionSummaries
      .AsNoTracking()
      .IgnoreAutoIncludes()
      .Select(x => new
      {
        x.CampaignName,
        x.ExecutionInfo,
        x.UniqueId,
        x.ExperimentSummaries
      })
      .ToArrayAsync((context?.CancellationToken ?? CancellationToken.None));
    var response = new AvailableCampaignExecutionSummariesResponse();
    response.AvailableCampaignSummaries
      .AddRange(summaries
      .Select(summary => new CampaignExecutionSummaryMetadata
      {
        CampaignName = summary.CampaignName,
        CompletionTime = summary.ExecutionInfo.TimeFinished,
        SummaryId = summary.UniqueId,
        NumExperiments = summary.ExperimentSummaries.Count
      }));

    return response;
  }

  public override async Task<CampaignExecutionSummary> GetCampaignSummary(CampaignExecutionSummaryRequest request, ServerCallContext? context)
  {
    await using var dbContext = await _coreContextFactory.CreateDbContextAsync();
    //Include.... EVERYTHING!
    var summary = await dbContext.CampaignExecutionSummaries
      .AsNoTracking()
      .AsSplitQuery()
      .FirstOrDefaultAsync(s => s.UniqueId == request.SummaryId, (context?.CancellationToken ?? CancellationToken.None));

    if(summary is null)
      //TODO: Figure out what to do here..?
      throw new InvalidOperationException("Couldn't locate a matching campaign summary!");

    return summary;
  }

  public override async Task<GetCopyOfCampaignResponse> GetCopyOfCampaign(CampaignRequest request, ServerCallContext context)
  {
    var response = new GetCopyOfCampaignResponse();
    var export = await _campaignTemplateTransferService.ExportAsync(request.UniqueId, context?.CancellationToken ?? CancellationToken.None);
    if(export is not null)
    {
      response.Template = export.Template;
      response.SerializedJsonData = export.Json;
    }

    return response;
  }

  private void HandleNotification(string title, string message, NotificationSeverityEnum severity)
  {
    foreach(var handler in _notificationHandlers)
    {
      handler.HandleNotification(title, message, severity);
    }
  }

  /// <summary>
  /// Returns a nested list of planner transactions, where each internal list represents the transactions with individual planners. 
  /// A response may for example be a list that contains two additional lists, where the first entry is a list of transactions with 
  /// planner A and the second a list of transactions with planner B.
  /// </summary>
  /// <returns>An enumerable of enumerables of <see cref="PlannerTransaction"/> from the start of the experiment to when this method is called. />
  public async Task<IEnumerable<IEnumerable<PlannerTransaction>?>> GetLatestPlanningTransactions()
  {
    if(_activeCampaignTemplateStore is null || _activeCampaignTemplateStore.CampaignTemplate is null || _executionManager.ExecutionStartTime is null)
      return [];

    var usedPlanners = _activeCampaignTemplateStore.CampaignTemplate.ExperimentTemplate.GetAllPlannedParameters()
      .Select(p => p.GetPlanningMetadata()?.PlannerName ?? "")
      .Where(name => !string.IsNullOrWhiteSpace(name))
      .Select(_plannerServiceRepo.GetPlannerByName)
      .Where(p => p is not null)
      .Distinct()
      .ToList();


    var listOfTransactions = new List<IEnumerable<PlannerTransaction>?>();

    foreach(var planner in usedPlanners)
    {

      var transactionRequest = new PlannerTransactionRequestFilter
      {
        PlannerId = planner?.UniqueId,
        Start = _executionManager.ExecutionStartTime?.ToTimestamp(),
        End = DateTime.UtcNow.ToTimestamp()
      };

      var transactions = await _plannerTransactionProvider.GetPlanningTransactionsAsync(transactionRequest);
      listOfTransactions.Add(transactions);
    }

    return listOfTransactions; 
  }

  /// <summary>
  /// Get's the latest list of analyzer transactions since the start of execution and the time this method is called.
  /// </summary>
  /// <returns>An enumerable of <see cref="AnalyzerTransaction"</see> logged between the start of execution and when this method was called./>
  public async Task<IEnumerable<AnalyzerTransaction>> GetLatestAnalyzerTransactions()
  {
    if(_activeCampaignTemplateStore is null || _activeCampaignTemplateStore.CampaignTemplate is null || _executionManager.ExecutionStartTime is null)
      return [];

    var filter = new AnalyzerTransactionRequestFilter
    {
      AnalyzerId = _activeCampaignTemplateStore.CampaignTemplate.ExperimentTemplate.AnalyzerId,
      Start = _executionManager.ExecutionStartTime?.ToTimestamp(),
      End = DateTime.UtcNow.ToTimestamp()
    };

    return await _analyzerTransactionProvider.GetAnalyzerTransactionsAsync(filter);
  }
}
