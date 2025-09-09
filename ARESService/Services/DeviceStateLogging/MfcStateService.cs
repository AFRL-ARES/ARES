using System.Linq;
using System.Threading.Tasks;
using Ares.Core.Device.State.Logging;
using Ares.Messages.DeviceState;
using Ares.Messages.DeviceStates;
using Ares.Messages.DeviceStates.Mfc;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;

namespace AresService.Services.DeviceStateLogging;

public class MfcStateService : MfcStateLogging.MfcStateLoggingBase
{
  readonly IDbContextFactory<AresDbContext> _dbContextFactory;
  public MfcStateService(IDbContextFactory<AresDbContext> dbContextFactory)
  {
    _dbContextFactory = dbContextFactory;
  }

  public override async Task<Empty> DeleteMfcStates(StateRequestFilter request, ServerCallContext context)
  {
    using var dbContext = _dbContextFactory.CreateDbContext();
    var stateQuery = await DeviceStateQueryBuilder.BuildQuery<MfcState>(request, dbContext);
    dbContext.MfcStates.RemoveRange(stateQuery);
    await dbContext.SaveChangesAsync();
    return new Empty();
  }

  public override async Task<MfcStateResponse> GetMfcStates(StateRequestFilter request, ServerCallContext context)
  {
    using var dbContext = _dbContextFactory.CreateDbContext();
    var response = new MfcStateResponse();
    var stateQuery = await DeviceStateQueryBuilder.BuildQuery<MfcState>(request, dbContext);
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
    var devices = await dbContext.MfcStates.GroupBy(s => s.DeviceId).Select(s => s.Key).ToListAsync();
    var response = new DevicesResponse();
    response.DeviceIds.AddRange(devices);
    return response;
  }
}
