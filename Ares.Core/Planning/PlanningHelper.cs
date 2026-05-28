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
    CancellationToken cancellationToken)
  {
    var parameterArray = parameters.ToArray();
    var plannerToMetadataMaps = new List<(IPlannerService Planner, ParameterMetadata Metadata)>();

    foreach(var plannerAllocation in plannerAllocations)
    {
      var planner = _plannerManager.GetPlannerById(plannerAllocation.Planner.UniqueId);
      if(planner is null)
        return false;

      plannerToMetadataMaps.Add((planner, plannerAllocation.Parameter));
    }

    var planGroup = plannerToMetadataMaps.GroupBy(pair => pair.Planner);
    var seedAnalysesArr = seedAnalyses.ToArray();
    foreach(var grouping in planGroup)
    {
      var planner = grouping.Key;
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
        var plannableParameters = grouping.Select(pair => pair.Metadata).ToArray();
        //make metadata thx
        planTransaction.TimeRequestSent = DateTime.UtcNow.ToTimestamp();
        
        //Create the plan request. Store it in the transaction.
        var planRequest = new PlanningRequest();
        planRequest.PlanningParameters.AddRange(plannableParameters.Select(parameter => ConvertToPlanningParameter(parameter, seedExperiments)));
        planRequest.AnalysisResults.AddRange(seedAnalysesArr.Select(a => (double)a.Result));
        planTransaction.PlanningRequest = planRequest;

        var planResponse = await planner.Plan(planRequest, cancellationToken);
        planTransaction.TimeResponseReceived = DateTime.UtcNow.ToTimestamp();
        planTransaction.PlanningResponse = planResponse;

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
      }
      catch(Exception e)
      {
        _logger.LogError("Failed to plan. {}", e);
        return false;
      }

      await LogPlannerTransaction(planTransaction);
    }

    return true;
  }

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

  private async Task LogPlannerTransaction(PlannerTransaction transaction)
  {
    var context = _dbContextFactory.CreateDbContext();
    await context.PlannerTransactions.AddAsync(transaction);
    await context.SaveChangesAsync();
  }
}
