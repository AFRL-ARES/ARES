using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ares.Core.Planning;
using Ares.Messaging;
using Ares.Messaging.Planning;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace Ares.Core.Grpc.Services;

public class PlanningService : AresPlanning.AresPlanningBase
{
  private readonly IPlannerManager _plannerManager;

  public PlanningService(IPlannerManager plannerManager)
  {
    _plannerManager = plannerManager;
  }

  public override Task<GetAllPlannersResponse> GetAllPlanners(GetAllPlannersRequest request, ServerCallContext context)
  {
    var response = new GetAllPlannersResponse();
    var planners = _plannerManager.AvailablePlanners.Select(planner => new PlannerInfo { Name = planner.Name, Version = planner.Version.ToString(), UniqueId = Guid.NewGuid().ToString(), Type = planner.GetType().Name });
    response.Planners.AddRange(planners);
    return Task.FromResult(response);
  }

  public override async Task<Empty> SeedManualPlanner(ManualPlannerSeed request, ServerCallContext context)
  {
    var planner = _plannerManager.GetPlanner<ManualPlanner>();
    await planner.Seed(request);

    return new Empty();
  }

  public override Task<ManualPlannerSetCollection> GetManualPlannerSeed(Empty request, ServerCallContext context)
  {
    var planner = _plannerManager.GetPlanner<ManualPlanner>();
    var seed = planner.CurrentPlanResults.ToArray();
    var test = seed.Select(tuples => tuples.Select(tuple => new ParameterNameValuePair { Name = tuple.Name, Value = tuple.Value }));
    var collection = ToManualPlannerSetCollection(test.Select(ToManualPlannerSet));
    return Task.FromResult(collection);
  }

  public override Task<Empty> ResetManualPlanner(Empty request, ServerCallContext context)
  {
    var planner = _plannerManager.GetPlanner<ManualPlanner>();
    planner.Reset();
    return Task.FromResult(new Empty());
  }

  private static ManualPlannerSet ToManualPlannerSet(IEnumerable<ParameterNameValuePair> pairs)
  {
    var set = new ManualPlannerSet();
    set.ParameterValues.AddRange(pairs);
    return set;
  }

  private static ManualPlannerSetCollection ToManualPlannerSetCollection(IEnumerable<ManualPlannerSet> sets)
  {
    var coll = new ManualPlannerSetCollection();
    coll.PlannedValues.AddRange(sets);
    return coll;
  }
}
