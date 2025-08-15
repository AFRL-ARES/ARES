using Ares.Messages.DeviceState;
using Ares.Messages.DeviceStates.Chiller;
using AresService.DeviceStateLoggers;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Ares.Messages.DeviceStates;

namespace AresService.Services.DeviceStateLogging;

public class LaserChillerStateService : ChillerStateLogging.ChillerStateLoggingBase
{
  readonly IDbContextFactory<AresDbContext> _dbContextFactory;

  public LaserChillerStateService(IDbContextFactory<AresDbContext> dbContextFactory)
  {
    _dbContextFactory = dbContextFactory;
  }

  public override async Task<Empty> DeleteStates(StateRequestFilter request, ServerCallContext context)
  {
    using var dbContext = _dbContextFactory.CreateDbContext();
    var stateQuery = await DeviceStateQueryBuilder.BuildQuery<ChillerState>(request, dbContext);
    dbContext.ChillerStates.RemoveRange(stateQuery);
    await dbContext.SaveChangesAsync();
    return new Empty();
  }

  public override async Task<ChillerStateResponse> GetStates(StateRequestFilter request, ServerCallContext context)
  {
    using var dbContext = _dbContextFactory.CreateDbContext();
    var response = new ChillerStateResponse();
    var stateQuery = await DeviceStateQueryBuilder.BuildQuery<ChillerState>(request, dbContext);
    var statesGroups = stateQuery.GroupBy(s => s.DeviceId);
    foreach(var group in statesGroups)
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
    var devices = await dbContext.ChillerStates.GroupBy(s => s.DeviceId).Select(s => s.Key).ToListAsync();
    var response = new DevicesResponse();
    response.DeviceIds.AddRange(devices);
    return response;
  }

}
