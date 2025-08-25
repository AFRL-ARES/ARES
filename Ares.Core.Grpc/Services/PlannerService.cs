using Ares.Core.Exceptions;
using Ares.Core.Planning;
using Ares.Datamodel;
using Ares.Datamodel.Connection;
using Ares.Datamodel.Planning;
using Ares.Services;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ares.Core.Grpc.Services;

public class PlannerService(IPlannerServiceRepo plannerRepo, IRemotePlannerManager plannerManager) : AresPlannerManagementService.AresPlannerManagementServiceBase
{
  private readonly IPlannerServiceRepo _plannerRepo = plannerRepo;
  private readonly IRemotePlannerManager _plannerManager = plannerManager;

  public override async Task<GetAllPlannersResponse> GetAllPlanners(Empty request, ServerCallContext context)
  {
    var response = new GetAllPlannersResponse();
    var availablePlanners = _plannerRepo.AvailablePlannerServices;
    var infos = await Task.WhenAll(availablePlanners.Select(GetInfo));
    response.Planners.AddRange(infos);
    return response;
  }

  private async Task<PlannerServiceInfo> GetInfo(IPlannerService planner)
  {
    var capabilities = await planner.GetCapabilities();

    var info = new PlannerServiceInfo
    {
      Name = planner.Name,
      Type = planner.Type,
      Version = planner.Version,
      Description = planner.Description,
      UniqueId = planner.UniqueId,
      Capabilities = capabilities,
      Address = planner is RemotePlannerService remotePlanner ? remotePlanner.Address.ToString() : string.Empty
    };

    return info;
  }

  public override async Task<AddPlannerResponse> AddPlanner(AddPlannerRequest request, ServerCallContext context)
  {
    var response = new AddPlannerResponse();
    try
    {
      await _plannerManager.CreatePlanner(request.Name, request.Address);
      response.Success = true;
    }

    catch(Exception e)
    {
      response.Success = false;
      response.ErrorMessage = e.Message;
    }

    return response;
  }

  public override async Task<UpdatePlannerResponse> UpdatePlanner(UpdatePlannerRequest request, ServerCallContext context)
  {
    var response = new UpdatePlannerResponse();
    try
    {
      var plannerConfig = new PlannerConfig() { UniqueId = request.PlannerId, Name = request.Name, Url = request.Url };
      await _plannerManager.UpdatePlanner(plannerConfig);
      response.Success = true;
    }

    catch(ItemNotFoundException e)
    {
      response.Success = false;
      response.ErrorMessage = e.Message;
    }

    return response;
  }

  public override async Task<Empty> RemovePlanner(RemovePlannerRequest request, ServerCallContext context)
  {
    await _plannerManager.RemovePlanner(request.PlannerId);
    return new Empty();
  }

  public override Task<StateResponse> GetState(StateRequest request, ServerCallContext context)
  {
    var response = new StateResponse();
    var planner = _plannerRepo.GetPlannerById(request.Id) ?? throw new ItemNotFoundException(request.Id, typeof(IPlannerService), "Failed to get state as planned was not found.");

    response.State = planner.PlannerServiceState;
    response.StateMessage = planner.StateMessage;

    return Task.FromResult(response);
  }

  public override async Task<PlannerInfoResponse> GetInfo(PlannerInfoRequest request, ServerCallContext context)
  {
    var planner = _plannerRepo.GetPlannerById(request.PlannerId);
    if(planner is null)
    {
      return new PlannerInfoResponse
      {
        Info = new PlannerServiceInfo { Name = "Unknown", Description = "Analyzer not found" }
      };
    }

    var info = await GetInfo(planner);
    var response = new PlannerInfoResponse { Info = info };

    return response;
  }

  public override Task<AresStruct> GetPlannerSettings(PlannerSettingsRequest request, ServerCallContext context)
  {
    var planner = _plannerRepo.GetPlannerById(request.PlannerId) ?? throw new ItemNotFoundException(request.PlannerId, typeof(IPlannerService), "Failed to get settings as requested planner was not found");
    return Task.FromResult(planner.PlannerServiceSettings);
  }

  public override async Task<Empty> SetPlannerSettings(PlannerSettings request, ServerCallContext context)
  {
    var planner = _plannerRepo.GetPlannerById(request.PlannerId);

    if(planner is null)
      return new Empty();

    if(planner is RemotePlannerService remotePlanner)
      await _plannerManager.UpdatePlannerSettings(request);

    else
      planner.UpdateSettings(request.Settings);

    return new Empty();
  }

  public override Task<ManualPlannerSetCollection> GetManualPlannerSeed(Empty request, ServerCallContext context)
  {
    var manualPlanner = _plannerRepo.GetManualPlanner() as ManualPlanner;

    if(manualPlanner is null)
      throw new InvalidOperationException("Cannot find the manual planner! How did this even happen..?");

    var seed = manualPlanner.CurrentPlanResults.ToArray();
    var test = seed.Select(tuples => tuples.Select(tuple => new ParameterNameValuePair { Name = tuple.Name, Value = tuple.Value }));
    var collection = ToManualPlannerSetCollection(test.Select(ToManualPlannerSet));
    return Task.FromResult(collection);
  }

  public override Task<Empty> ResetManualPlanner(Empty request, ServerCallContext context)
  {
    var manualPlanner = _plannerRepo.GetManualPlanner() as ManualPlanner;

    if(manualPlanner is null)
      throw new InvalidOperationException("Cannot find the manual planner! How did this even happen..?");

    manualPlanner.Reset();
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
