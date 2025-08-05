using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ares.Messages.DeviceState;
using AresService.DeviceStateLoggers;
using AresMessaging.DeviceStateLogging;
using Microsoft.EntityFrameworkCore;

namespace AresService.DeviceStateExport.StateGetters;

public class DeviceStateGetter : IDeviceStateGetter
{
  private readonly IDbContextFactory<AresDbContext> _dbContextFactory;

  public DeviceStateGetter(IDbContextFactory<AresDbContext> dbContextFactory)
  {
    _dbContextFactory = dbContextFactory;
  }

  public async Task<IDictionary<string, IEnumerable<TState>>> GetStates<TState>(StateRequestFilter request) where TState : class, IDeviceState
  {
    using var context = _dbContextFactory.CreateDbContext();
    var stateQuery = await DeviceStateQueryBuilder.BuildQuery<TState>(request, context);
    var statesGroups = stateQuery.GroupBy(s => s.DeviceId);
    var stateMap = statesGroups.ToDictionary(k => k.Key, v => v.AsEnumerable());
    return stateMap;
  }
}