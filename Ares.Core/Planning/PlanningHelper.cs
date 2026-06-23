using Ares.Core.Execution.Extensions;
using Ares.Core.Notifications;
using Ares.Datamodel;
using Ares.Datamodel.Analyzing;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Planning;
using Ares.Datamodel.Templates;
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Ares.Core.Planning;

public class PlanningHelper : IPlanningHelper
{
  private readonly IPlannerServiceRepo _plannerManager;
  private readonly ILogger<PlanningHelper> _logger;
  private readonly INotifier _notifier;
  private readonly IDbContextFactory<CoreDatabaseContext> _dbContextFactory;

  public PlanningHelper(IPlannerServiceRepo plannerManager, 
    ILogger<PlanningHelper> logger, 
    INotifier notifier, 
    IDbContextFactory<CoreDatabaseContext> dbContextFactory)
  {
    _plannerManager = plannerManager;
    _logger = logger;
    _notifier = notifier;
    _dbContextFactory = dbContextFactory;
  }

  public async Task<bool> TryResolveParameters(IEnumerable<PlannerAllocation> plannerAllocations,
    RequestMetadata metadata,
    IEnumerable<Parameter> parameters,
    IEnumerable<Analysis> seedAnalyses,
    IEnumerable<ExperimentOverview> seedExperiments,
    int batchSize,
    List<PlanStatusCode> statusCodes,
    CancellationToken cancellationToken)
  {
    var parameterArray = parameters.ToArray();
    var plannerToMetadataMaps = MapParameterMetadataToPlanners(plannerAllocations);

    if(plannerToMetadataMaps is null)
      return false;

    var planGroup = plannerToMetadataMaps.GroupBy(pair => pair.Planner);
    var seedAnalysesArr = seedAnalyses.ToArray();

    var planningTasks = planGroup.Select(async grouping =>
    {
      var planner = grouping.Key;

      var planQueue = PlannerQueueDictionary.GetOrAdd(planner.UniqueId, _ => new ConcurrentQueue<Plan>());

      if(planQueue.TryDequeue(out var plan))
      {
        ResolveParametersFromPlan(plan, parameterArray);
        return true;
      }

      return await RequestNewPlans(planner, grouping, seedExperiments, seedAnalysesArr, statusCodes, metadata, planQueue, parameterArray, batchSize, cancellationToken);
    });

    var results = await Task.WhenAll(planningTasks);
    return results.All(success => success);
  }

  /// <summary>
  /// Requests a fresh set of plans from a planner service, resolves using the first one received and queues any additional.
  /// </summary>
  /// <param name="planner">The planner service to request plans from</param>
  /// <param name="grouping">The grouping of planner and parameter metadata</param>
  /// <param name="seedExperiments">Seed experiments for the planning request</param>
  /// <param name="seedAnalysesArr">Seed analyses for the planning request</param>
  /// <param name="statusCode">The status code of the plan</param>
  /// <param name="metadata">Request metadata</param>
  /// <param name="planQueue"></param>
  /// <param name="parameterArray"></param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  private async Task<bool> RequestNewPlans(IPlannerService planner, 
    IGrouping<IPlannerService, (IPlannerService, ParameterMetadata)> grouping,
    IEnumerable<ExperimentOverview> seedExperiments,
    Analysis[] seedAnalysesArr,
    List<PlanStatusCode> statusCodes,
    RequestMetadata metadata,
    ConcurrentQueue<Plan> planQueue,
    Parameter[] parameterArray,
    int batchSize,
    CancellationToken cancellationToken)
  {
    var planTransaction = new PlannerTransaction()
    {
      UniqueId = Guid.NewGuid().ToString(),
      PlannerName = planner.Name,
      PlannerType = planner.Type,
      PlannerVersion = planner.Version,
      PlannerId = planner.UniqueId
    };

    try
    {
      var plannableParameters = grouping.Select(pair => pair.Item2).ToArray();
      planTransaction.TimeRequestSent = DateTime.UtcNow.ToTimestamp();

      //Create the plan request. Store it in the transaction.
      var planRequest = CreatePlanningRequest(plannableParameters, seedExperiments, seedAnalysesArr, statusCodes, batchSize, metadata);
      planTransaction.PlanningRequest = planRequest;

      var planResponse = await planner.Plan(planRequest, cancellationToken);
      planTransaction.TimeResponseReceived = DateTime.UtcNow.ToTimestamp();
      planTransaction.PlanningResponse = planResponse;

      if(planResponse.Plans.Any())
      {
        var currentPlan = planResponse.Plans.First();

        foreach(var plan in planResponse.Plans.Skip(1))
          planQueue.Enqueue(plan);

        ResolveParametersFromPlan(currentPlan, parameterArray);
      }

      else
        await HandleLegacyPlanResponse(planResponse, parameterArray);
    }

    catch(Exception e)
    {
      _logger.LogError("Failed to plan. {}", e.Message);
      await _notifier.Notify("Planner Error!", e.Message, NotificationSeverityEnum.Error);
      return false;
    }

    await LogPlannerTransaction(planTransaction);
    return true;
  }

  /// <summary>
  /// Creates a planning request based on the provided data.
  /// </summary>
  /// <param name="plannableParameters">The parameters to be planned for</param>
  /// <param name="seedExperiments">Seed data to pull experiment history from</param>
  /// <param name="seedAnalysesArr">Analysis results to be included in the planning request</param>
  /// <param name="statusCode">The previous plans status code</param>
  /// <param name="metadata">Request metadata</param>
  /// <returns>An ARES <cref><see cref="PlanningRequest"/></cref></returns>
  private PlanningRequest CreatePlanningRequest(ParameterMetadata[] plannableParameters, 
    IEnumerable<ExperimentOverview> seedExperiments, 
    Analysis[] seedAnalysesArr, 
    List<PlanStatusCode> statusCodes,
    int batchSize,
    RequestMetadata metadata)
  {
    //Create the plan request. Store it in the transaction.
    var planRequest = new PlanningRequest();
    planRequest.PlanningParameters.AddRange(plannableParameters.Select(parameter => ConvertToPlanningParameter(parameter, seedExperiments)));
    planRequest.AnalysisResults.AddRange(seedAnalysesArr.Select(a => (double)a.Result));
    planRequest.PreviousPlanStatusCodes.AddRange(statusCodes);
    planRequest.Metadata = metadata;
    planRequest.BatchSize = batchSize;

    return planRequest;
  }

  /// <summary>
  /// Uses the provided planner allocations to map parameter metadata to their respective planners.
  /// </summary>
  /// <param name="plannerAllocations"></param>
  /// <returns>A mapping of planners to the provided parameter metadata.</returns>
  private List<(IPlannerService Planner, ParameterMetadata Metadata)>? MapParameterMetadataToPlanners(IEnumerable<PlannerAllocation> plannerAllocations)
  {
    var plannerToMetadataMaps = new List<(IPlannerService Planner, ParameterMetadata Metadata)>();

    foreach(var plannerAllocation in plannerAllocations)
    {
      var planner = _plannerManager.GetPlannerById(plannerAllocation.Planner.UniqueId);

      if(planner is null)
        return null;

      plannerToMetadataMaps.Add((planner, plannerAllocation.Parameter));
    }

    return plannerToMetadataMaps;
  }

  /// <summary>
  /// Handles legacy plan responses
  /// </summary>
  /// <param name="planResponse"></param>
  /// <param name="parameterArray"></param>
  /// <returns>A bool indicating whether or not resolving variables was a success</returns>
  private async Task<bool> HandleLegacyPlanResponse(PlanningResponse planResponse, Parameter[] parameterArray)
  {
    #pragma warning disable CS0612 // Type or member is obsolete

    if(planResponse.PlanningOutcome == Outcome.Failure)
    {
      if(string.IsNullOrWhiteSpace(planResponse.ErrorString))
        await _notifier.Notify("Planner Error!", "Planner reported that planning failed, but did not provide any specific error as to why.", NotificationSeverityEnum.Error);

      else
        await _notifier.Notify($"Planner Reported Error: {planResponse.ErrorString}", "Planner Error!", NotificationSeverityEnum.Error);

      return false;
    }

    if(planResponse.PlanningOutcome == Outcome.Warning)
    {
      if(string.IsNullOrWhiteSpace(planResponse.ErrorString))
        await _notifier.Notify("Planner Warning", "Planner reported a warning, but did not provide specific context for that warning.", NotificationSeverityEnum.Warning);

      else
        await _notifier.Notify("Planner Warning", $"Planner successfully planned, but reported a warning: {planResponse.ErrorString}", NotificationSeverityEnum.Warning);
    }

    if(planResponse.PlanningOutcome == Outcome.Canceled)
      await _notifier.Notify("Planning was canceled.", "Planning was canceled.", NotificationSeverityEnum.Info);

    if(!planResponse.PlannedParameters.Any())
    {
      await _notifier.Notify("Planning Error!", "Tried to plan for experiment, but planning returned no plan results! Campaign will stop.", NotificationSeverityEnum.Error);
      return false;
    }

    foreach(var result in planResponse.PlannedParameters)
    {
      var parameterPlanTarget = parameterArray.FirstOrDefault(parameter => parameter.GetPlanningMetadata()?.Name == result.ParameterName);

      if(parameterPlanTarget is null)
        continue;

      parameterPlanTarget.SetResolvedValue(result.ParameterValue);
    }

    return true;
    #pragma warning restore CS0612 // Type or member is obsolete
  }

  /// <summary>
  /// Takes in a plan and a parameter array and resolves those parameters using the provided plan
  /// </summary>
  /// <param name="plan">The plan containing the planned parameters</param>
  /// <param name="parameterArray">The array of parameters to be resolved</param>
  private void ResolveParametersFromPlan(Plan plan, Parameter[] parameterArray)
  {
    foreach(var result in plan.PlannedParameters)
    {
      var parameterPlanTarget = parameterArray.FirstOrDefault(parameter => parameter.GetPlanningMetadata()?.Name == result.ParameterName);

      if(parameterPlanTarget is null)
        continue;

      parameterPlanTarget.SetResolvedValue(result.ParameterValue);
    }
  }

  /// <summary>
  /// Taks in a piece of parameter metadata and the experiment history to create an ARES planning parameter.
  /// </summary>
  /// <param name="metadata"></param>
  /// <param name="experimentHistory"></param>
  /// <returns>An ARES <see cref="PlanningParameter"/></returns>
  private static PlanningParameter ConvertToPlanningParameter(ParameterMetadata metadata, IEnumerable<ExperimentOverview> experimentHistory)
  {
    var parameter = new PlanningParameter
    {
      ParameterName = metadata.Name,
      IsPlanned = true,
      DataType = metadata.Schema.Type,
      InitialValue = metadata.InitialValue
    };

    var paramHistory = experimentHistory.Select(exp =>
    {
      var plannedParameters = exp.Template.GetAllPlannedParameters();
      var plannedValue = plannedParameters.FirstOrDefault(param => param.GetPlanningMetadata()?.Name == metadata.Name)?.GetValue();

      var actualValue = string.IsNullOrEmpty(metadata.OutputName) ? null : exp.Result.Fields.FirstOrDefault(f => f.Key == metadata.OutputName).Value;

      if(plannedValue is null)
        return new ParameterHistoryInfo();

      else
        return new ParameterHistoryInfo
        {
          PlannedValue = plannedValue,
          AchievedValue = actualValue ?? AresValueHelper.CreateNull()
        };
    });

    parameter.ParameterHistory.AddRange(paramHistory);

    if(metadata.Constraints.Any())
    {
      var constraint = metadata.Constraints.First();
      parameter.MinimumValue = constraint.Minimum;
      parameter.MaximumValue = constraint.Maximum;
    }

    parameter.PlannerName = metadata.PlannerName;
    return parameter;
  }

  /// <summary>
  /// Logs the planner transaction to the database.
  /// </summary>
  /// <param name="transaction"></param>
  /// <returns></returns>
  private async Task LogPlannerTransaction(PlannerTransaction transaction)
  {
    using var context = _dbContextFactory.CreateDbContext();
    await context.PlannerTransactions.AddAsync(transaction);
    await context.SaveChangesAsync();
  }

  /// <summary>
  /// Reseeds the manual planner with it's last provided seed data, essentially returning it to a state as if new seed data had just been provided.
  /// </summary>
  /// <returns></returns>
  public async Task ReseedManualPlanner()
  {
    var manualPlanner = _plannerManager.GetManualPlanner();

    if(manualPlanner is not null)
      await manualPlanner.Reseed();
  }

  private ConcurrentDictionary<string, ConcurrentQueue<Plan>> PlannerQueueDictionary { get; set; } = new ConcurrentDictionary<string, ConcurrentQueue<Plan>>();
}
