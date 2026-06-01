using Ares.Core.Device.Repos;
using Ares.Core.Device.State.Logging;
using Ares.Datamodel.Device;
using Microsoft.EntityFrameworkCore;

namespace Ares.Core.Device.State.Export.StateGetters;

public class DeviceStateGetter : IDeviceStateGetter
{
  private readonly IDbContextFactory<CoreDatabaseContext> _dbContextFactory;
  private readonly IAresDeviceRepo _deviceRepo;

  public DeviceStateGetter(IAresDeviceRepo deviceRepo, IDbContextFactory<CoreDatabaseContext> dbContextFactory)
  {
    _dbContextFactory = dbContextFactory;
    _deviceRepo = deviceRepo;
  }

  public async Task<IDictionary<string, TState[]>> GetStates<TState>(DeviceStateRequestFilter request, CancellationToken token) where TState : class, IDeviceState
  {
    using var context = _dbContextFactory.CreateDbContext();
    var stateQuery = await DeviceStateQueryBuilder.BuildQuery<TState>(request, context);
    var stateMap = await stateQuery
      .GroupBy(s => s.DeviceId)
      .ToDictionaryAsync(g => _deviceRepo.First(d => d.UniqueId == g.Key).Name, g => g.ToArray(), token);

    return stateMap;
  }
}
