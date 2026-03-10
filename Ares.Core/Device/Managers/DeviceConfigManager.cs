using Ares.Core.Device.Repos;
using Ares.Datamodel.Device;
using Microsoft.EntityFrameworkCore;

namespace Ares.Core.Device.Managers;

public class DeviceConfigManager : IDeviceConfigManager
{
  private readonly IDbContextFactory<CoreDatabaseContext> _dbContextFactory;
  private IDeviceConfigRepo _configRepo;

  public DeviceConfigManager(IDbContextFactory<CoreDatabaseContext> dbContextFactory, IDeviceConfigRepo configRepo)
  {
    _dbContextFactory = dbContextFactory;
    _configRepo = configRepo;
  }

  public async Task LoadConfigs()
  {
    await using var context = _dbContextFactory.CreateDbContext();
    var existingDeviceConfigs = await context.DeviceConfigs.ToListAsync();
    existingDeviceConfigs.ForEach(_configRepo.AddOrUpdate);
  }

  public async Task Add(DeviceConfig config)
  {
    //Initialize important ID's
    config.UniqueId = Guid.NewGuid().ToString();
    config.DeviceId = Guid.NewGuid().ToString();

    //Add to storage mechanisms
    await using var context = _dbContextFactory.CreateDbContext();
    var existingDeviceConfig = await context.DeviceConfigs.FirstOrDefaultAsync(existingConfig => existingConfig.UniqueId == config.UniqueId);
    if(existingDeviceConfig is not null)
      throw new InvalidOperationException($"A device with id {config.UniqueId} already exists in the configuration database");

    context.DeviceConfigs.Add(config);
    await context.SaveChangesAsync();
    _configRepo.AddOrUpdate(config);
  }

  public async Task Remove(string id)
  {
    await using var context = _dbContextFactory.CreateDbContext();
    var genericConfig = await context.DeviceConfigs.FirstOrDefaultAsync(config => config.UniqueId == id);
    if(genericConfig is null)
      return;

    context.DeviceConfigs.Remove(genericConfig);
    await context.SaveChangesAsync();
    _configRepo.Remove(id);
  }

  public async Task Update(string id, DeviceConfig config)
  {
    await using var context = _dbContextFactory.CreateDbContext();
    var existingConfig = await context.DeviceConfigs.FirstOrDefaultAsync(c => c.UniqueId == id);
    if(existingConfig is null)
      return;

    existingConfig.DeviceName = config.DeviceName;
    existingConfig.SerialInfo = config.SerialInfo;
    existingConfig.DeviceSettings = config.DeviceSettings;
    existingConfig.DriverId = config.DriverId;
    existingConfig.IsSimulated = config.IsSimulated;

    await context.SaveChangesAsync();
    _configRepo.AddOrUpdate(existingConfig);
  }
}
