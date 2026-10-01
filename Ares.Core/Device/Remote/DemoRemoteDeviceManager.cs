using Ares.Core.Device.Repos;
using Ares.Core.Device.State.Logging;
using Ares.Core.Execution.VersionChecking;
using Ares.Core.Notifications;
using Ares.Datamodel.Device;
using Microsoft.Extensions.Logging;

namespace Ares.Core.Device.Remote;

public class DemoRemoteDeviceManager : IRemoteDeviceManager
{
  private readonly IAresDeviceRepo _deviceRepo;
  private readonly IDeviceCache _deviceCache;
  private readonly INotificationHandler _notificationHandler;
  private readonly StateLoggerManager _stateLoggerManager;
  private readonly IDatamodelVersionValidator _datamodelVersionValidator;
  private readonly ILogger<DemoRemoteDeviceManager> _logger;
  private readonly ILoggerFactory _loggerFactory;
  private readonly List<RemoteDeviceMonitor> _deviceMonitors = [];


  public DemoRemoteDeviceManager(IAresDeviceRepo deviceRepo, 
    IDeviceCache deviceCache, 
    INotificationHandler notificationHandler, 
    StateLoggerManager stateLoggerManager, 
    IDatamodelVersionValidator datamodelVersionValidator,
    ILogger<DemoRemoteDeviceManager> logger,
    ILoggerFactory loggerFactory)
  {
    _deviceRepo = deviceRepo;
    _deviceCache = deviceCache;
    _notificationHandler = notificationHandler;
    _stateLoggerManager = stateLoggerManager;
    _datamodelVersionValidator = datamodelVersionValidator;
    _logger = logger;
    _loggerFactory = loggerFactory;
  }
  public Task<RemoteDevice?> CreateDevice(string name, string url)
    => throw new InvalidOperationException("Remote Device Cannot be Added in Demo Mode!");

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
    var existingDemo = _deviceRepo.GetDevice(DemoIds.RemoteDeviceId);
    if(existingDemo is null)
    {
      try
      {
        var demoDevice = await LoadExistingDevice(new RemoteDeviceConfig
        {
          Name = DemoIds.RemoteDeviceName,
          UniqueId = DemoIds.RemoteDeviceId,
          Url = "http://localhost:5257"
        });

        if(demoDevice is null)
          return;

        _deviceRepo.AddOrUpdate(demoDevice);
        var demoMonitor = new RemoteDeviceMonitor(demoDevice, _deviceCache, _loggerFactory.CreateLogger<RemoteDeviceMonitor>());
        _deviceMonitors.Add(demoMonitor);

        await _stateLoggerManager.SetupLogger(demoDevice);
      }
      catch(Exception ex)
      {
        _logger.LogError(ex, "Failed to initialize demo remote device {DeviceName}", DemoIds.RemoteDeviceName);
      }
    }
  }

  private async Task<RemoteDevice?> LoadExistingDevice(RemoteDeviceConfig config)
  {
    var device = ConfigToDevice(config);
    if(device is null)
      return null;

    var deviceInfo = await _deviceCache.GetCachedDeviceInfo(config.UniqueId);
    if(deviceInfo is not null)
      await device.UpdateInfo(deviceInfo);

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

  public Task<bool> RemoveDevice(string deviceId)
    => Task.FromResult(true);

  public Task UpdateDevice(RemoteDeviceConfig config)
    => Task.CompletedTask;

  public Task UpdateDeviceSettings(DeviceSettings deviceSettings)
    => Task.CompletedTask;
}
