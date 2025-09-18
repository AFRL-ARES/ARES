using System.Linq;
using System.Threading.Tasks;
using Ares.Core.Device.Helpers;
using Ares.Core.Device.State.Logging;
using Ares.Datamodel.Device;
using Ares.Messages.DeviceStates;
using Ares.Messages.DeviceStates.RestSerialDevice;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using RestSerialDevice;

namespace AresService.Services.DeviceStateLogging
{
  public class RestSerialDeviceStateService : RestSerialDeviceStateLogging.RestSerialDeviceStateLoggingBase
  {
    readonly IDbContextFactory<AresDbContext> _dbContextFactory;
    private readonly DeviceIdHelper _deviceIdHelper;

    public RestSerialDeviceStateService(IDbContextFactory<AresDbContext> dbContextFactory, DeviceIdHelper deviceIdHelper)
    {
      _dbContextFactory = dbContextFactory;
      _deviceIdHelper = deviceIdHelper;
    }

    public override async Task<Empty> DeleteRestSerialDeviceStates(DeviceStateRequestFilter request, ServerCallContext context)
    {
      using var dbContext = _dbContextFactory.CreateDbContext();
      var stateQuery = await DeviceStateQueryBuilder.BuildQuery<RestSerialDeviceStateEntity>(request, dbContext);
      dbContext.RestSerialDeviceStates.RemoveRange(stateQuery);
      await dbContext.SaveChangesAsync();
      return new Empty();
    }

    public override async Task<RestSerialDeviceStateResponse> GetRestSerialDeviceStates(DeviceStateRequestFilter request, ServerCallContext context)
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
      var deviceIds = await dbContext.DeviceConfigs
        .Where(s => s.DeviceType == typeof(ISerialRestDevice).FullName)
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
