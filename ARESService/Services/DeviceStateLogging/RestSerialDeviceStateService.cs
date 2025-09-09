using Ares.Messages.DeviceState;
using Ares.Messages.DeviceStates.RestSerialDevice;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Ares.Messages.DeviceStates;
using Ares.Core.Device.State.Logging;

namespace AresService.Services.DeviceStateLogging
{
  public class RestSerialDeviceStateService : RestSerialDeviceStateLogging.RestSerialDeviceStateLoggingBase
  {
    readonly IDbContextFactory<AresDbContext> _dbContextFactory;

    public RestSerialDeviceStateService(IDbContextFactory<AresDbContext> dbContextFactory)
    {
      _dbContextFactory = dbContextFactory;
    }

    public override async Task<Empty> DeleteRestSerialDeviceStates(StateRequestFilter request, ServerCallContext context)
    {
      using var dbContext = _dbContextFactory.CreateDbContext();
      var stateQuery = await DeviceStateQueryBuilder.BuildQuery<RestSerialDeviceStateEntity>(request, dbContext);
      dbContext.RestSerialDeviceStates.RemoveRange(stateQuery);
      await dbContext.SaveChangesAsync();
      return new Empty();
    }

    public override async Task<RestSerialDeviceStateResponse> GetRestSerialDeviceStates(StateRequestFilter request, ServerCallContext context)
    {
      using var dbContext = _dbContextFactory.CreateDbContext();
      var response = new RestSerialDeviceStateResponse();
      var stateQuery = await DeviceStateQueryBuilder.BuildQuery<RestSerialDeviceStateEntity>(request, dbContext);
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
      var devices = await dbContext.RestSerialDeviceStates.GroupBy(s => s.DeviceId).Select(s => s.Key).ToListAsync();
      var response = new DevicesResponse();
      response.DeviceIds.AddRange(devices);
      return response;
    }
  }
}
