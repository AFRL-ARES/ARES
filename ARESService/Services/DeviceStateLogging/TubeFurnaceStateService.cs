using System.Linq;
using System.Threading.Tasks;
using Ares.Core.Device.State.Logging;
using Ares.Messages.DeviceState;
using Ares.Messages.DeviceStates;
using Ares.Messages.DeviceStates.TubeFurnace;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;

namespace AresService.Services.DeviceStateLogging;

public class TubeFurnaceStateService : TubeFurnaceStateLogging.TubeFurnaceStateLoggingBase
{
  readonly IDbContextFactory<AresDbContext> _dbContextFactory;
  public TubeFurnaceStateService(IDbContextFactory<AresDbContext> dbContextFactory)
  {
    _dbContextFactory = dbContextFactory;
  }

  public override async Task<Empty> DeleteTubeFurnaceStates(StateRequestFilter request, ServerCallContext context)
  {
    using var dbContext = _dbContextFactory.CreateDbContext();
    var stateQuery = await DeviceStateQueryBuilder.BuildQuery<TubeFurnaceStateEntity>(request, dbContext);
    dbContext.TubeFurnaceStates.RemoveRange(stateQuery);
    await dbContext.SaveChangesAsync();
    return new Empty();
  }

  public override async Task<TubeFurnaceStateResponse> GetTubeFurnaceStates(StateRequestFilter request, ServerCallContext context)
  {
    using var dbContext = _dbContextFactory.CreateDbContext();
    var response = new TubeFurnaceStateResponse();
    var stateQuery = await DeviceStateQueryBuilder.BuildQuery<TubeFurnaceStateEntity>(request, dbContext);
    var statesGroups = stateQuery.GroupBy(s => s.DeviceId);
    foreach (var group in statesGroups)
    {
      var collection = new StateCollection();
      collection.StateLogs.AddRange(group);
      response.StateMap[group.Key] = collection;
    }

    return response;
  }

  public override async Task<DevicesResponse> GetAvailableDevices(Empty request, ServerCallContext context)
  {
    using var dbContext = _dbContextFactory.CreateDbContext();
    var devices = await dbContext.TubeFurnaceStates.GroupBy(s => s.DeviceId).Select(s => s.Key).ToListAsync();
    var response = new DevicesResponse();
    response.DeviceIds.AddRange(devices);
    return response;
  }
}
