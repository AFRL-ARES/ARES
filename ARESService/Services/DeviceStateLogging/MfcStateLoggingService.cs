using Ares.Messages.DeviceStates;
using Ares.Messages.DeviceStates.Mfc;
using ARESCore;
using ARESCore.DeviceStateLoggers;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace ARESService.Services.DeviceStateLogging;

public class MfcStateLoggingService : MfcStateLogging.MfcStateLoggingBase
{
  readonly IDbContextFactory<ARESDbContext> _dbContextFactory;
  public MfcStateLoggingService(IDbContextFactory<ARESDbContext> dbContextFactory)
  {
    _dbContextFactory = dbContextFactory;
  }

  public override async Task<Empty> DeleteMfcStates(StateRequest request, ServerCallContext context)
  {
    using var dbContext = _dbContextFactory.CreateDbContext();
    var stateQuery = await DeviceStateFilterBuilder.BuildFilter<MfcState>(request, dbContext);
    dbContext.MfcStates.RemoveRange(stateQuery);
    await dbContext.SaveChangesAsync();
    return new Empty();
  }

  public override async Task<MfcStateResponse> GetMfcStates(StateRequest request, ServerCallContext context)
  {
    using var dbContext = _dbContextFactory.CreateDbContext();
    var response = new MfcStateResponse();
    var stateQuery = await DeviceStateFilterBuilder.BuildFilter<MfcState>(request, dbContext);
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
    var devices = await dbContext.MfcStates.GroupBy(s => s.DeviceId).Select(s => s.Key).ToListAsync();
    var response = new DevicesResponse();
    response.DeviceIds.AddRange(devices);
    return response;
  }
}
