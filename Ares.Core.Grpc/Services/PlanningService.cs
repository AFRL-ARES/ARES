using Ares.Core.Notifications;
using Ares.Core.Planning;
using Ares.Messaging;
using Ares.Messaging.Planning;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ares.Core.Grpc.Services;

public class PlanningService : AresPlanning.AresPlanningBase
{
  private readonly IPlannerManager _plannerManager;
  private readonly IDbContextFactory<CoreDatabaseContext> _coreContextFactory;
  private readonly INotificationHandler _notificationHandler;

  public PlanningService(IPlannerManager plannerManager, IDbContextFactory<CoreDatabaseContext> coreContextFactory, INotificationHandler notificationHandler)
  {
    _plannerManager = plannerManager;
    _coreContextFactory = coreContextFactory;
    _notificationHandler = notificationHandler;
  }

  public override Task<GetAllPlannersResponse> GetAllPlanners(Empty request, ServerCallContext context)
  {
    var response = new GetAllPlannersResponse();
    var planners = _plannerManager.AvailablePlanners.Select(planner => new PlannerAdapterInfo { AdapterName = planner.Name, Version = planner.Version.ToString(), UniqueId = Guid.NewGuid().ToString(), Type = planner.GetType().Name, Address = planner.Address });
    response.Planners.AddRange(planners);
    return Task.FromResult(response);
  }

  public override Task<CapabilitiesResponse> GetPlannerCapabilities(CapabilitiesRequest request, ServerCallContext context)
  {
    var planner = _plannerManager.AvailablePlanners.FirstOrDefault(p => p.Name == request.AdapterName);
    var response = new CapabilitiesResponse();

    if(planner is not null)
      response.PlannerCapability.AddRange(planner.AvailablePlanners.Select(p => new PlannerOption() { Name = p.PlannerName, Description = p.Description, Version = p.Version }));

    return Task.FromResult(response);
  }

  public override Task<PlannerStatus> GetPlannerStatus(PlannerStatusRequest request, ServerCallContext context)
  {
    var planner = _plannerManager.AvailablePlanners.FirstOrDefault(p => p.Name == request.AdapterName);

    if(planner is null)
      return Task.FromResult(new PlannerStatus { PlannerState = PlannerState.Error, Message = "ARES was unable to find this planner!" });

    return Task.FromResult(planner.Status);
  }

  public override async Task<Empty> ActivatePlanner(PlannerActivationRequest request, ServerCallContext context)
  {
    var planner = _plannerManager.AvailablePlanners.FirstOrDefault(p => p.Name == request.AdapterName);

    if(planner is null)
      return new Empty();

    await planner.Init();
    return new Empty();
  }

  public override Task<PlannerSettingsResponse> GetPlannerSettings(PlannerSettingsRequest request, ServerCallContext context)
  {
    var planner = _plannerManager.AvailablePlanners.FirstOrDefault(p => p.Name == request.ServiceName);
    var response = new PlannerSettingsResponse();

    if(planner is null)
      return Task.FromResult(response);

    response.Settings.AddRange(planner.AdapterSettings);
    return Task.FromResult(response);
  }

  public override async Task<Empty> AddPlanner(GenericPlanner request, ServerCallContext context)
  {
    if(_plannerManager.AvailablePlanners.Any(p => p.Name == request.Name))
      return new Empty();

    var uri = new Uri(request.Address);
    var planner = new Planning.AresPlanner.AresPlanner(request.Name, new Uri(request.Address));
    await planner.Init();
    await _plannerManager.RegisterPlanner(planner);
    await AddPlannerToDb(planner, context);
    return new Empty();
  }

  public override async Task<Empty> RemovePlanner(GenericPlanner request, ServerCallContext context)
  {
    var planner = _plannerManager.GetPlannerByName(request.Name);

    if(planner is null)
      return new Empty();

    await _plannerManager.UnregisterPlanner(planner);
    await RemovePlannerFromDb(request.Name, context);
    return new Empty();
  }

  public override async Task<Empty> UpdatePlanner(GenericPlanner request, ServerCallContext context)
  {
    var planner = _plannerManager.GetPlannerByName(request.Name);

    if(planner is null)
      return new Empty();

    await _plannerManager.UnregisterPlanner(planner);
    var updatedPlanner = new Planning.AresPlanner.AresPlanner(request.Name, new Uri(request.Address));
    await updatedPlanner.Init();
    await _plannerManager.RegisterPlanner(updatedPlanner);
    await RemovePlannerFromDb(planner.Name, context);
    await AddPlannerToDb(updatedPlanner, context);
    return new Empty();
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

  private async Task AddPlannerToDb(Planning.AresPlanner.AresPlanner planner, ServerCallContext context)
  {
    try
    {
      var info = new PlannerAdapterInfo()
      {
        AdapterName = planner.Name,
        Address = planner.Address,
        Type = planner.GetType().ToString(),
        Version = planner.Version.ToString(),
        UniqueId = planner.UniqueId
      };

      await using var dbContext = await _coreContextFactory.CreateDbContextAsync();
      await dbContext.Planners.AddAsync(info);
      await dbContext.SaveChangesAsync(context.CancellationToken);
    }

    catch(Exception ex)
    {
      await HandleNotification("Failed to Add Planner to Database", ex.Message, NotificationSeverityEnum.Error);
    }

  }

  private async Task RemovePlannerFromDb(string name, ServerCallContext context)
  {
    try
    {
      await using var dbContext = await _coreContextFactory.CreateDbContextAsync();

      var oldInfo = await dbContext.Planners.FirstOrDefaultAsync(a => a.AdapterName == name);
      if(oldInfo != null)
        dbContext.Planners.Remove(oldInfo);
      await dbContext.SaveChangesAsync(context.CancellationToken);
    }

    catch(Exception ex)
    {
      await HandleNotification("Failed to Remove Planner from Database", ex.Message, NotificationSeverityEnum.Error);

    }
  }

  private async Task HandleNotification(string title, string message, NotificationSeverityEnum severity)
  {
    await _notificationHandler.HandleNotification(title, message, severity);
  }
}
