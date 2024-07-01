using Ares.Messaging.Device;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;

namespace Ares.Core.Device;

public abstract class DeviceConfigManagerBase<TConfig, TDevice> : IDeviceConfigManager<TConfig> where TConfig : IMessage, new()
{
  private readonly IDbContextFactory<CoreDatabaseContext> _dbContextFactory;

  public DeviceConfigManagerBase(IDbContextFactory<CoreDatabaseContext> dbContextFactory)
  {
    _dbContextFactory = dbContextFactory;
  }

  public async Task Add(string id, TConfig config)
  {
    await using var context = _dbContextFactory.CreateDbContext();
    var existingDeviceConfig = await context.DeviceConfigs.FirstOrDefaultAsync(deviceConfig => deviceConfig.DeviceName == id);
    if (existingDeviceConfig is not null)
      throw new InvalidOperationException($"A device with id {id} already exists in the configuration database");

    var newConfig = new DeviceConfig
    {
      DeviceName = id,
      DeviceType = typeof(TDevice).FullName,
      UniqueId = Guid.NewGuid().ToString(),
      ConfigData = Any.Pack(config)
    };

    context.DeviceConfigs.Add(newConfig);
    await context.SaveChangesAsync();
  }

  public async Task Remove(string id)
  {
    await using var context = _dbContextFactory.CreateDbContext();
    var genericConfig = await context.DeviceConfigs.FirstOrDefaultAsync(config => config.DeviceName == id);
    if (genericConfig is null)
      return;

    context.DeviceConfigs.Remove(genericConfig);
    await context.SaveChangesAsync();
  }

  public async Task Update(string id, TConfig config)
  {
    await using var context = _dbContextFactory.CreateDbContext();
    var genericConfig = await context.DeviceConfigs.FirstOrDefaultAsync(config => config.DeviceName == id);
    if (genericConfig is null)
      return;

    genericConfig.ConfigData = Any.Pack(config);
    await context.SaveChangesAsync();
  }

  public async Task<TConfig?> Get(string id)
  {
    await using var context = _dbContextFactory.CreateDbContext();
    var genericConfig = await context.DeviceConfigs.FirstOrDefaultAsync(config => config.DeviceName == id);
    if (genericConfig is null)
      return default;

    var config = genericConfig.ConfigData.Unpack<TConfig>();
    return config;
  }
}
