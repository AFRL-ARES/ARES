using Ares.Messages.DeviceStates;
using Ares.Messages.DeviceStates.Tc0304;
using ARESCore;
using ARESCore.DeviceStateLoggers;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace ARESService.Services.DeviceStateLogging;

public class Tc0304StateLoggingService : Tc0304StateLogging.Tc0304StateLoggingBase
{
  readonly IDbContextFactory<ARESDbContext> _dbContextFactory;
  public Tc0304StateLoggingService(IDbContextFactory<ARESDbContext> dbContextFactory)
  {
    _dbContextFactory = dbContextFactory;
  }

  public override async Task<Empty> DeleteStates(StateRequest request, ServerCallContext context)
  {
    using var dbContext = _dbContextFactory.CreateDbContext();
    var stateQuery = await DeviceStateFilterBuilder.BuildFilter<Tc0304State>(request, dbContext);
    dbContext.Tc0304States.RemoveRange(stateQuery);
    await dbContext.SaveChangesAsync();
    return new Empty();
  }

  public override async Task<Tc0304StateResponse> GetStates(StateRequest request, ServerCallContext context)
  {
    using var dbContext = _dbContextFactory.CreateDbContext();
    var response = new Tc0304StateResponse();
    var stateQuery = await DeviceStateFilterBuilder.BuildFilter<Tc0304State>(request, dbContext);
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
    var devices = await dbContext.Tc0304States.GroupBy(s => s.DeviceId).Select(s => s.Key).ToListAsync();
    var response = new DevicesResponse();
    response.DeviceIds.AddRange(devices);
    return response;
  }
}
