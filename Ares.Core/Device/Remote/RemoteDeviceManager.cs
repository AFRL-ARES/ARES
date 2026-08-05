using Ares.Core.Device.Repos;
using Ares.Core.Device.State.Logging;
using Ares.Core.Execution.VersionChecking;
using Ares.Core.Notifications;
using Ares.Datamodel.Device;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ares.Core.Device.Remote;

internal class RemoteDeviceManager(
  IAresDeviceRepo _deviceRepo,
  IDeviceCache _deviceCache,
  INotificationHandler _notificationHandler,
  IDbContextFactory<CoreDatabaseContext> _dbContextFactory,
  StateLoggerManager _stateLoggerManager,
  ILoggerFactory _loggerFactory,
  IDatamodelVersionValidator _datamodelVersionValidator,
  ILogger<RemoteDeviceManager> _logger) : IRemoteDeviceManager
{
  private readonly List<RemoteDeviceMonitor> _deviceMonitors = [];

  public async Task<RemoteDevice?> CreateDevice(string name, string url)
  {
    var config = new RemoteDeviceConfig { UniqueId = Guid.NewGuid().ToString(), Name = name, Url = url };
    var device = ConfigToDevice(config);
    var activated = await device.Activate(CancellationToken.None);

    if(activated)
    {
      _deviceRepo.AddOrUpdate(device);

      var monitor = new RemoteDeviceMonitor(device, _deviceCache, _loggerFactory.CreateLogger<RemoteDeviceMonitor>());
      _deviceMonitors.Add(monitor);

      var ctx = _dbContextFactory.CreateDbContext();
      ctx.RemoteDeviceConfigs.Add(config);

      await _stateLoggerManager.SetupLogger(device);

      await ctx.SaveChangesAsync();
      return device;
    }

    return null;
  }

  private RemoteDevice ConfigToDevice(RemoteDeviceConfig config)
  {
    var uriValid = Uri.TryCreate(config.Url, UriKind.Absolute, out var uri);
    if(!uriValid || uri is null)
    {
      _logger.LogError("Failed to load a remote device {DeviceName} because the url {DeviceUrl} is invalid.", config.Name, config.Url);
      _ = _notificationHandler.HandleNotification(
        "Device Load Error",
        $"Failed to load a remote device {config.Name} because the url {config.Url} is invalid.",
        NotificationSeverityEnum.Danger);
      throw new InvalidOperationException($"Failed to load a remote device {config.Name} because the url {config.Url} is invalid.");
    }

    var remoteInfo = new RemoteConnectionInfo
    {
      Address = config.Url,
      ConnectionInfo = new DeviceConnectionInfo
      {
        DeviceId = config.UniqueId,
        DeviceName = config.Name,
        Simulated = false,
      }
    };

    var logger = _loggerFactory.CreateLogger<RemoteDevice>();
    var device = new RemoteDevice(remoteInfo, logger, _notificationHandler, _datamodelVersionValidator);
    return device;
  }

  public async Task LoadDevices()
  {
    var ctx = _dbContextFactory.CreateDbContext();
    var configs = await ctx.RemoteDeviceConfigs.ToArrayAsync();
    var devices = await Task.WhenAll(configs.Select(LoadExistingDevice));
    var nonNullDevices = devices.OfType<RemoteDevice>().ToArray();
    foreach(var device in nonNullDevices)
    {
      _deviceRepo.AddOrUpdate(device);
      var monitor = new RemoteDeviceMonitor(device, _deviceCache, _loggerFactory.CreateLogger<RemoteDeviceMonitor>());
      _deviceMonitors.Add(monitor);

      await _stateLoggerManager.SetupLogger(device);
    }
  }

  public async Task<bool> RemoveDevice(string deviceId)
  {
    var ctx = _dbContextFactory.CreateDbContext();
    var device = ctx.RemoteDeviceConfigs.Where(a => a.UniqueId == deviceId).FirstOrDefault();
    if(device is null)
    {
      return false;
    }

    _deviceRepo.Remove(deviceId);
    ctx.Remove(device);
    await ctx.SaveChangesAsync();

    var monitor = _deviceMonitors.First(m => m.DeviceId == deviceId);
    monitor.Dispose();
    _deviceMonitors.Remove(monitor);

    await _stateLoggerManager.RemoveLogger(device.UniqueId, removeSettings: true);

    return true;
  }

  public async Task UpdateDevice(RemoteDeviceConfig config)
  {
    var ctx = _dbContextFactory.CreateDbContext();
    var deviceCfg = ctx.RemoteDeviceConfigs.Where(a => a.UniqueId == config.UniqueId).FirstOrDefault();
    if(deviceCfg is null)
    {
      return;
    }

    deviceCfg.Name = config.Name;
    deviceCfg.Url = config.Url;
    await ctx.SaveChangesAsync();

    var monitor = _deviceMonitors.First(m => m.DeviceId == deviceCfg.UniqueId);
    monitor.Dispose();
    _deviceMonitors.Remove(monitor);
    await _stateLoggerManager.RemoveLogger(deviceCfg.UniqueId);

    var device = await LoadExistingDevice(deviceCfg);
    if(device is null)
    {
      return;
    }

    _deviceRepo.Remove(config.UniqueId);
    _deviceRepo.AddOrUpdate(device);

    await _stateLoggerManager.SetupLogger(device);

    monitor = new RemoteDeviceMonitor(device, _deviceCache, _loggerFactory.CreateLogger<RemoteDeviceMonitor>());
    _deviceMonitors.Add(monitor);
  }

  public async Task UpdateDeviceSettings(DeviceSettings deviceSettings)
  {
    var remoteDevice = _deviceRepo.OfType<RemoteDevice>().FirstOrDefault(d => d.UniqueId == deviceSettings.DeviceId);
    if(remoteDevice is null)
      return;
    

    await remoteDevice.UpdateSettings(deviceSettings.Settings);
    await _deviceCache.CacheDeviceSettings(remoteDevice);
  }

  private async Task<RemoteDevice?> LoadExistingDevice(RemoteDeviceConfig config)
  {
    var device = ConfigToDevice(config);
    if(device is null)
      return null;

    var deviceInfo = await _deviceCache.GetCachedDeviceInfo(config.UniqueId);
    if(deviceInfo is not null)
    {
      await device.UpdateInfo(deviceInfo);
    }

    await device.Activate(CancellationToken.None);

    var deviceSettings = await _deviceCache.GetCachedDeviceSettings(config.UniqueId);
    if(deviceSettings is not null && deviceSettings.Fields.Count != 0)
    {
      try
      {
        await device.UpdateSettings(deviceSettings);
      }

      catch(Exception ex) 
      {
        _logger.LogError(ex.Message);
      }
    }

    await _deviceCache.CacheDeviceInfo(device);
    await _deviceCache.CacheDeviceSettings(device);

    return device;
  }
}
