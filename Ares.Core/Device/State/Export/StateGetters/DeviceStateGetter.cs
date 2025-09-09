using Ares.Core.Device.State.Logging;
using Ares.Datamodel.Device;
using Microsoft.EntityFrameworkCore;

namespace Ares.Core.Device.State.Export.StateGetters;

public class DeviceStateGetter : IDeviceStateGetter
{
  private readonly IDbContextFactory<CoreDatabaseContext> _dbContextFactory;

  public DeviceStateGetter(IDbContextFactory<CoreDatabaseContext> dbContextFactory)
  {
    _dbContextFactory = dbContextFactory;
  }

  public async Task<IDictionary<string, IEnumerable<TState>>> GetStates<TState>(DeviceStateRequestFilter request) where TState : class, IDeviceState
  {
    using var context = _dbContextFactory.CreateDbContext();
    var stateQuery = await DeviceStateQueryBuilder.BuildQuery<TState>(request, context);
    var statesGroups = stateQuery.GroupBy(s => s.DeviceId);
    var stateMap = statesGroups.ToDictionary(k => k.Key, v => v.AsEnumerable());
    return stateMap;
  }
}