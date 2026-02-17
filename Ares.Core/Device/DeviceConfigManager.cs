using Ares.Datamodel.Device;
using Microsoft.EntityFrameworkCore;

namespace Ares.Core.Device;

public class DeviceConfigManager : IDeviceConfigManager
{
  private readonly IDbContextFactory<CoreDatabaseContext> _dbContextFactory;

  public DeviceConfigManager(IDbContextFactory<CoreDatabaseContext> dbContextFactory)
  {
    _dbContextFactory = dbContextFactory;
  }

  public async Task Add(string id, string name, DeviceConfig config)
  {
    await using var context = _dbContextFactory.CreateDbContext();
    var existingDeviceConfig = await context.DeviceConfigs.FirstOrDefaultAsync(deviceConfig => deviceConfig.UniqueId == id);
    if(existingDeviceConfig is not null)
      throw new InvalidOperationException($"A device with id {id} already exists in the configuration database");

    context.DeviceConfigs.Add(config);
    await context.SaveChangesAsync();
  }

  public async Task Remove(string id)
  {
    await using var context = _dbContextFactory.CreateDbContext();
    var genericConfig = await context.DeviceConfigs.FirstOrDefaultAsync(config => config.UniqueId == id);
    if(genericConfig is null)
      return;

    context.DeviceConfigs.Remove(genericConfig);
    await context.SaveChangesAsync();
  }

  public async Task Update(string id, DeviceConfig config)
  {
    await using var context = _dbContextFactory.CreateDbContext();
    var genericConfig = await context.DeviceConfigs.FirstOrDefaultAsync(config => config.UniqueId == id);
    if(genericConfig is null)
      return;

    genericConfig = config;
    await context.SaveChangesAsync();
  }

  public async Task<DeviceConfig?> Get(string id)
  {
    await using var context = _dbContextFactory.CreateDbContext();
    return await context.DeviceConfigs.FirstOrDefaultAsync(config => config.UniqueId == id);
  }
}
