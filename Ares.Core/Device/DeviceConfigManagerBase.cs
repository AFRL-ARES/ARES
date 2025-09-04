using Ares.Datamodel.Device;
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

  public async Task Add(string id, string name, TConfig config)
  {
    await using var context = _dbContextFactory.CreateDbContext();
    var existingDeviceConfig = await context.DeviceConfigs.FirstOrDefaultAsync(deviceConfig => deviceConfig.UniqueId == id);
    if(existingDeviceConfig is not null)
      throw new InvalidOperationException($"A device with id {id} already exists in the configuration database");

    var newConfig = new DeviceConfig
    {
      DeviceName = name,
      DeviceType = typeof(TDevice).FullName,
      UniqueId = id,
      ConfigData = Any.Pack(config)
    };

    context.DeviceConfigs.Add(newConfig);
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

  public async Task Update(string id, TConfig config)
  {
    await using var context = _dbContextFactory.CreateDbContext();
    var genericConfig = await context.DeviceConfigs.FirstOrDefaultAsync(config => config.UniqueId == id);
    if(genericConfig is null)
      return;

    var packed = Any.Pack(config);
    genericConfig.ConfigData.Value = packed.Value;
    genericConfig.ConfigData.TypeUrl = packed.TypeUrl;
    await context.SaveChangesAsync();
  }

  public async Task<TConfig?> Get(string id)
  {
    await using var context = _dbContextFactory.CreateDbContext();
    var genericConfig = await context.DeviceConfigs.FirstOrDefaultAsync(config => config.UniqueId == id);
    if(genericConfig is null)
      return default;

    var config = genericConfig.ConfigData.Unpack<TConfig>();
    return config;
  }
}
