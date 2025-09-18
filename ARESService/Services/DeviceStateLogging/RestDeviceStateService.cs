using System.Linq;
using System.Threading.Tasks;
using Ares.Core.Device.Helpers;
using Ares.Core.Device.State.Logging;
using Ares.Datamodel.Device;
using Ares.Messages.DeviceStates;
using Ares.Messages.DeviceStates.RestDevice;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using RestDevice;

namespace AresService.Services.DeviceStateLogging
{
  public class RestDeviceStateService : RestDeviceStateLogging.RestDeviceStateLoggingBase
  {
    readonly IDbContextFactory<AresDbContext> _dbContextFactory;
    private readonly DeviceIdHelper _deviceIdHelper;

    public RestDeviceStateService(IDbContextFactory<AresDbContext> dbContextFactory, DeviceIdHelper deviceIdHelper)
    {
      _dbContextFactory = dbContextFactory;
      _deviceIdHelper = deviceIdHelper;
    }

    public override async Task<Empty> DeleteRestDeviceStates(DeviceStateRequestFilter request, ServerCallContext context)
    {
      using var dbContext = _dbContextFactory.CreateDbContext();
      var stateQuery = await DeviceStateQueryBuilder.BuildQuery<RestDeviceStateEntity>(request, dbContext);
      dbContext.RestDeviceStates.RemoveRange(stateQuery);
      await dbContext.SaveChangesAsync();
      return new Empty();
    }

    public override async Task<RestDeviceStateResponse> GetRestDeviceSttates(DeviceStateRequestFilter request, ServerCallContext context)
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
      var deviceIds = await dbContext.DeviceConfigs
        .Where(s => s.DeviceType == typeof(IRestDevice).FullName)
        .Select(s => s.UniqueId)
        .ToListAsync();

      var deviceDescriptions = deviceIds.Select(id => new DevicesDescription
        { DeviceId = id, DeviceName = _deviceIdHelper.DeviceIdToName(id) }).ToList();
      var response = new DevicesResponse();
      response.Devices.AddRange(deviceDescriptions);
      return response;
    }
  }
}
