using Ares.Device;
using ARESCore;
using ARESCore.DeviceManagers;
using Google.Protobuf;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace ARESCore.DeviceDbLoaders;

public abstract class DeviceDbLoaderBase<TDevice, TConfig> : IDeviceDbLoader where TDevice : IAresDevice where TConfig : IMessage, new()
{
  private readonly IDbContextFactory<ARESDbContext> _dbContextFactory;
  private readonly IDeviceManager<TConfig, TDevice> _deviceManager;

  public DeviceDbLoaderBase(IDbContextFactory<ARESDbContext> dbContextFactory, IDeviceManager<TConfig, TDevice> deviceManager)
  {
    _dbContextFactory = dbContextFactory;
    _deviceManager = deviceManager;
  }

  public virtual async Task Load()
  {
    await using var context = _dbContextFactory.CreateDbContext();
    var deviceConfigs = await context.DeviceConfigs
      .Where(config => config.DeviceType == typeof(TDevice).FullName)
      .Select(config => config.ConfigData.Unpack<TConfig>()).ToArrayAsync();
    await _deviceManager.Load(deviceConfigs);
  }
}
