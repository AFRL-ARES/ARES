using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Ares.Core.Analyzing;
using Ares.Core.Execution;
using Ares.Core.Execution.StartConditions;
using Ares.Core.Execution.StopConditions;
using Ares.Core.Grpc.Helpers;
using Ares.Messaging;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;

namespace Ares.Core.Grpc.Services;

public class AutomationService : AresAutomation.AresAutomationBase
{
  private readonly IActiveCampaignTemplateStore _activeCampaignTemplateStore;
  private readonly IAnalyzerManager _analyzerManager;
  private readonly IDbContextFactory<CoreDatabaseContext> _coreContextFactory;
  private readonly IExecutionManager _executionManager;
  private readonly IExecutionReportStore _executionReportStore;
  private readonly IEnumerable<IStartCondition> _startConditions;
  readonly IDesiredAnalysisResultFactory _desiredAnalysisResultFactory;

  public AutomationService(IDbContextFactory<CoreDatabaseContext> coreContextFactory,
    IExecutionManager executionManager,
    IExecutionReportStore executionReportStore,
    IActiveCampaignTemplateStore activeCampaignTemplateStore,
    IEnumerable<IStartCondition> startConditions,
    IAnalyzerManager analyzerManager,
    IDesiredAnalysisResultFactory desiredAnalysisResultFactory)
  {
    _desiredAnalysisResultFactory = desiredAnalysisResultFactory;
    _coreContextFactory = coreContextFactory;
    _executionManager = executionManager;
    _executionReportStore = executionReportStore;
    _activeCampaignTemplateStore = activeCampaignTemplateStore;
    _startConditions = startConditions;
    _analyzerManager = analyzerManager;
  }


  public override async Task<ProjectsResponse> GetAllProjects(Empty request, ServerCallContext context)
  {
    await using var dbContext = await _coreContextFactory.CreateDbContextAsync();
    var projects = await dbContext.Projects.AsNoTracking().ToArrayAsync(context.CancellationToken);
    var response = new ProjectsResponse();
    response.Projects.AddRange(projects);
    return response;
  }

  public override async Task<CampaignsResponse> GetAllCampaigns(Empty request, ServerCallContext context)
  {
    await using var dbContext = _coreContextFactory.CreateDbContext();
    var campaignsResponse = new CampaignsResponse();
    var campaigns = await dbContext.CampaignTemplates.AsNoTracking().ToArrayAsync(context.CancellationToken);
    campaignsResponse.CampaignTemplates.Add(campaigns);
    return campaignsResponse;
  }

  public override async Task<BoolValue> CampaignExists(CampaignRequest request, ServerCallContext context)
  {
    await using var dbContext = _coreContextFactory.CreateDbContext();
    await dbContext.Database.OpenConnectionAsync();
    bool exists;
    if (!string.IsNullOrEmpty(request.UniqueId))
      exists = await dbContext.CampaignTemplates.AsNoTracking().AnyAsync(template => template.UniqueId == request.UniqueId, context.CancellationToken);
    else
      exists = await dbContext.CampaignTemplates.AsNoTracking().AnyAsync(template => template.Name == request.CampaignName, context.CancellationToken);

    return new BoolValue { Value = exists };
  }

  public override async Task<Project> GetProject(ProjectRequest request, ServerCallContext context)
  {
    await using var dbContext = _coreContextFactory.CreateDbContext();
    return await dbContext.Projects.AsNoTracking().FirstAsync(project => project.Name == request.ProjectName, context.CancellationToken);
  }

  public override Task<CampaignTemplate> GetSingleCampaign(CampaignRequest request, ServerCallContext context)
    => GetCampaignTemplate(request, context);

  public override async Task<Empty> RemoveCampaign(CampaignRequest request, ServerCallContext context)
  {
    var campaignTemplate = await GetCampaignTemplate(request, context);
    await using var dbContext = _coreContextFactory.CreateDbContext();
    dbContext.CampaignTemplates.Remove(campaignTemplate);
    await dbContext.SaveChangesAsync(context.CancellationToken);

    return new Empty();
  }

  public override async Task<Empty> RemoveProject(ProjectRequest request, ServerCallContext context)
  {
    await using var dbContext = _coreContextFactory.CreateDbContext();
    var project = await dbContext.Projects.FirstAsync(p => p.Name == request.ProjectName, context.CancellationToken);
    dbContext.Projects.Remove(project);
    await dbContext.SaveChangesAsync(context.CancellationToken);

    return new Empty();
  }

  public override async Task<Empty> AddProject(Project request, ServerCallContext context)
  {
    await using var dbContext = _coreContextFactory.CreateDbContext();
    dbContext.Projects.Add(request);
    await dbContext.SaveChangesAsync(context.CancellationToken);
    return new Empty();
  }

  /// <summary>
  /// </summary>
  /// <param name="request">
  ///   <see
  /// </param>
  /// <param name="context"></param>
  /// <returns></returns>
  public override async Task<Empty> AddCampaign(CampaignTemplate request, ServerCallContext context)
  {
    await using var dbContext = _coreContextFactory.CreateDbContext();
    dbContext.CampaignTemplates.Add(request);
    await dbContext.SaveChangesAsync(context.CancellationToken);
    return new Empty();
  }

  public override async Task<CampaignTemplate> UpdateCampaign(CampaignTemplate request, ServerCallContext context)
  {
    await using var dbContext = _coreContextFactory.CreateDbContext();

    var existingCampaign = await dbContext.CampaignTemplates.FirstAsync(template => template.UniqueId == request.UniqueId);
    dbContext.CampaignTemplates.Remove(existingCampaign);
    await dbContext.SaveChangesAsync();
    dbContext.ChangeTracker.Clear();
    request.ConsolidatePlannedParameterMetadata();
    try
    {
      dbContext.CampaignTemplates.Add(request);
    }
    catch (Exception ex)
    {
      dbContext.CampaignTemplates.Add(existingCampaign);
      Console.WriteLine(ex.ToString());
    }
    // existingCampaign.UpdateCampaignTemplate(request, dbContext);
    // await dbContext.SaveChangesAsync();
    // dbContext.ChangeTracker.Clear();
    // dbContext.CampaignTemplates.Update(request);
    await dbContext.SaveChangesAsync();
    // var currentTemplate = await dbContext.CampaignTemplates.FirstAsync(template => template.Name == request.CampaignName, context.CancellationToken);
    // currentTemplate.Name = request.CampaignTemplate.Name;
    // currentTemplate.PlannableParameters.Clear();
    // currentTemplate.PlannableParameters.Add(request.CampaignTemplate.PlannableParameters);
    // currentTemplate.ExperimentTemplates.Clear();
    // currentTemplate.ExperimentTemplates.Add(request.CampaignTemplate.ExperimentTemplates);
    return request;
  }

  private async Task<CampaignTemplate> GetCampaignTemplate(CampaignRequest request, ServerCallContext context)
  {
    await using var dbContext = _coreContextFactory.CreateDbContext();
    if (!string.IsNullOrEmpty(request.UniqueId))
      return await dbContext.CampaignTemplates.AsNoTracking().FirstAsync(template => template.UniqueId == request.UniqueId, context.CancellationToken);

    return await dbContext.CampaignTemplates.AsNoTracking().FirstAsync(template => template.Name == request.CampaignName);
  }

  public override Task<CampaignResponse> GetCurrentlySelectedCampaign(Empty request, ServerCallContext context)
  {
    var template = _activeCampaignTemplateStore.CampaignTemplate;
    var response = new CampaignResponse { HasValue = template is not null, Value = template };
    return Task.FromResult(response);
  }

  public override Task<Empty> StartExecution(Empty request, ServerCallContext context)
  {
    _executionManager.Start();
    return Task.FromResult(new Empty());
  }

  public override async Task<CampaignTemplate> SetCampaignForExecution(CampaignRequest request, ServerCallContext context)
  {
    var template = await GetCampaignTemplate(request, context);
    _activeCampaignTemplateStore.CampaignTemplate = template;
    return template;
  }

  public override Task GetExecutionStatusStream(Empty request, IServerStreamWriter<ExperimentExecutionStatus> responseStream, ServerCallContext context)
  {
    var observable = _executionReportStore.ExperimentStatusObservable;
    return observable.Where(status => status is not null).Do(status => responseStream.WriteAsync(status!)).ToTask(context.CancellationToken);
  }

  public override Task<CampaignExecutionStatusResponse> GetCampaignExecutionStatus(Empty request, ServerCallContext context)
  {
    var status = _executionReportStore.CampaignExecutionStatus;
    return Task.FromResult(new CampaignExecutionStatusResponse
    {
      Status = status
    });
  }

  public override Task<Empty> StopExecution(Empty request, ServerCallContext context)
  {
    _executionManager.Stop();
    return Task.FromResult(new Empty());
  }

  public override Task<Empty> PauseExecution(Empty request, ServerCallContext context)
  {
    _executionManager.Pause();
    return Task.FromResult(new Empty());
  }

  public override Task<Empty> ResumeExecution(Empty request, ServerCallContext context)
  {
    _executionManager.Resume();
    return Task.FromResult(new Empty());
  }

  public override Task<StartStopConditionsResponse> GetAssignedStopConditions(Empty request, ServerCallContext context)
  {
    var conditions = _executionManager.CampaignStopConditions;
    var response = new StartStopConditionsResponse();
    var startStopConditions = conditions?.Select(condition => new StartStopCondition { Message = condition.Message, Name = condition.GetType().Name }) ?? new List<StartStopCondition>();
    response.StartStopConditions.AddRange(startStopConditions);

    return Task.FromResult(response);
  }

  public override Task<StartStopConditionsResponse> GetFailedStartConditions(Empty request, ServerCallContext context)
  {
    var response = new StartStopConditionsResponse();
    var conditions = _startConditions.Select(condition => condition.CanStart()).Where(result => result is not null && !result.Success).Select(condition => new StartStopCondition { Message = string.Join(Environment.NewLine, condition!.Messages), Name = condition.GetType().Name });
    response.StartStopConditions.AddRange(conditions);

    return Task.FromResult(response);
  }

  public override Task<Empty> RemoveStopCondition(StartStopCondition request, ServerCallContext context)
  {
    var stopConditions = _executionManager.CampaignStopConditions;
    if (stopConditions is null)
      return Task.FromResult(new Empty());

    var condition = stopConditions.FirstOrDefault(condition => condition.GetType().Name.Equals(request.Name));
    if (condition is not null)
      stopConditions.Remove(condition);

    return Task.FromResult(new Empty());
  }

  public override async Task<AvailableCampaignResultsResponse> GetAvailableCampaignResults(Empty request, ServerCallContext context)
  {
    await using var dbContext = _coreContextFactory.CreateDbContext();
    var results = dbContext.CampaignResults.Select(result => new CampaignResultMetadata
    {
      CompletionTime = result.ExecutionInfo.TimeFinished,
      ResultId = result.UniqueId
    }).ToArray();

    var response = new AvailableCampaignResultsResponse();
    response.AvailableCampaignResults.AddRange(results);

    return response;
  }

  public override async Task<CampaignResult> GetCampaignResult(CampaignResultRequest request, ServerCallContext context)
  {
    await using var dbContext = _coreContextFactory.CreateDbContext();
    var result = dbContext.CampaignResults.First(campaignResult => campaignResult.UniqueId == request.ResultId);

    return result;
  }

  public override Task<GetAllAnalyzersResponse> GetAllAnalyzers(Empty request, ServerCallContext context)
  {
    var response = new GetAllAnalyzersResponse();
    var analyzers = _analyzerManager.AvailableAnalyzers.Select(analyzer => new AnalyzerInfo { Name = analyzer.Name, Type = analyzer.GetType().Name, Version = analyzer.Version.ToString(), UniqueId = Guid.NewGuid().ToString() });
    response.Analyzers.AddRange(analyzers);
    return Task.FromResult(response);
  }

  public override Task<StartStopConditionsResponse> GetPreliminaryFailedStartConditions(CampaignTemplate request, ServerCallContext context)
  {
    var response = new StartStopConditionsResponse();
    var conditions = _startConditions.Select(condition => condition.CanStart()).Where(result => result is not null && !result.Success).Select(condition => new StartStopCondition { Message = string.Join(Environment.NewLine, condition!.Messages), Name = condition.GetType().Name });
    response.StartStopConditions.AddRange(conditions);

    return Task.FromResult(response);
  }

  public override Task<Empty> SetNumExperimentsStopCondition(NumExperimentsCondition request, ServerCallContext context)
  {
    var stopConditions = _executionManager.CampaignStopConditions;
    if (stopConditions is null)
      return Task.FromResult(new Empty());

    //var existingStopCondition = stopConditions.FirstOrDefault(condition => condition.GetType().Name.Equals(nameof(NumExperimentsCondition)));
    //if (existingStopCondition is not null)
    //  stopConditions.Remove(existingStopCondition);

    stopConditions.Clear();

    var newCondition = new NumExperimentsRun(_executionReportStore, request.NumExperiments);
    stopConditions.Add(newCondition);

    return Task.FromResult(new Empty());
  }

  public override Task<Empty> SetAnalysisResultStopCondition(AnalysisResultCondition request, ServerCallContext context)
  {
    var stopConditions = _executionManager.CampaignStopConditions;
    if (stopConditions is null)
      return Task.FromResult(new Empty());

    stopConditions.Clear();

    var stopCondition = _desiredAnalysisResultFactory.Create(request.DesiredResult, request.Leeway);
    stopConditions.Add(stopCondition);

    return Task.FromResult(new Empty());
  }

  public override Task<ExperimentStopConditionResponse> GetActiveStopCondition(Empty request, ServerCallContext context)
  {
    var stopConditions = _executionManager.CampaignStopConditions;
    if (stopConditions is null || !stopConditions.Any())
    {
      return Task.FromResult(
        new ExperimentStopConditionResponse
        {
          ActiveCondition = "None",
          Description = "No stop conditions assigned, experiment will run until manually stopped."
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
}
