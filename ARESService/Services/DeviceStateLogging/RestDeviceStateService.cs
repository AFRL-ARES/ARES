using Ares.Messages.DeviceState;
using Ares.Messages.DeviceStates;
using Ares.Messages.DeviceStates.RestDevice;
using AresService;
using AresService.DeviceStateLoggers;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace AresService.Services.DeviceStateLogging
{
  public class RestDeviceStateService : RestDeviceStateLogging.RestDeviceStateLoggingBase
  {
    readonly IDbContextFactory<AresDbContext> _dbContextFactory;

    public RestDeviceStateService(IDbContextFactory<AresDbContext> dbContextFactory)
    {
      _dbContextFactory = dbContextFactory;
    }

    public override async Task<Empty> DeleteRestDeviceStates(StateRequestFilter request, ServerCallContext context)
    {
      using var dbContext = _dbContextFactory.CreateDbContext();
      var stateQuery = await DeviceStateQueryBuilder.BuildQuery<RestDeviceStateEntity>(request, dbContext);
      dbContext.RestDeviceStates.RemoveRange(stateQuery);
      await dbContext.SaveChangesAsync();
      return new Empty();
    }

    public override async Task<RestDeviceStateResponse> GetRestDeviceSttates(StateRequestFilter request, ServerCallContext context)
    {
      using var dbContext = _dbContextFactory.CreateDbContext();
      var response = new RestDeviceStateResponse();
      var stateQuery = await DeviceStateQueryBuilder.BuildQuery<RestDeviceStateEntity>(request, dbContext);
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
      var devices = await dbContext.RestDeviceStates.GroupBy(s => s.DeviceId).Select(s => s.Key).ToListAsync();
      var response = new DevicesResponse();
      response.DeviceIds.AddRange(devices);
      return response;
    }
  }
}
