using Ares.Core.Device.Plugins.Drivers;
using Ares.Core.Device.Providers;
using Ares.Core.Device.Repos;
using Ares.Core.Notifications;
using Ares.Core.Resources;
using Ares.Datamodel.Device;
using Ares.Device;
using DynamicData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;

namespace Ares.Core.Device.Managers;

public class DeviceManager : IDeviceManager
{
  private readonly IDeviceDriverProvider _driverProvider;
  private readonly IDeviceConfigProvider _configProvider;
  private readonly IDeviceConfigManager _configManager;
  private readonly IDriverDatabaseManager _driverDatabaseManager;
  private readonly INotificationHandler _notificationHandler;
  private readonly IAresDeviceRepo _deviceRepo;
  private readonly IServiceProvider _serviceProvider;
  private readonly ILoggerFactory _loggerFactory;
  private readonly ILogger<DeviceManager> _logger;
  private readonly IResourceConnectionArbiter _resourceConnectionArbiter;
  private readonly CompositeDisposable _cleanup = new();

  public DeviceManager(
    IDeviceDriverProvider driverProvider,
    IAresDeviceRepo deviceRepository,
    IDriverDatabaseManager driverDatabaseManager,
    IDeviceConfigManager deviceConfigManager,
    INotificationHandler notificationHandler,
    IDeviceConfigProvider configProvider,
    IServiceProvider serviceProvider,
    ILoggerFactory loggerFactory,
    IResourceConnectionArbiter resourceConnectionArbiter)
  {
    _driverProvider = driverProvider;
    _deviceRepo = deviceRepository;
    _driverDatabaseManager = driverDatabaseManager;
    _configManager = deviceConfigManager;
    _notificationHandler = notificationHandler;
    _configProvider = configProvider;
    _serviceProvider = serviceProvider;
    _loggerFactory = loggerFactory;
    _logger = loggerFactory.CreateLogger<DeviceManager>();
    _resourceConnectionArbiter = resourceConnectionArbiter;
  }

  public void Initialize()
  {
    _configProvider.Connect()
      .SelectMany(async changes =>
      {
        foreach(var change in changes)
        {
          await HandleChangeAsync(change);
        }
        return Unit.Default;
      })
      .Subscribe()
      .DisposeWith(_cleanup);
  }

  private async Task HandleChangeAsync(Change<DeviceConfig, string> change)
  {
    switch(change.Reason)
    {
      case ChangeReason.Add:
        await Create(change.Current);
        break;
      case ChangeReason.Update:
        await Update(change.Current.DeviceId, change.Current);
        break;
      case ChangeReason.Remove:
        await Remove(change.Current.DeviceId);
        break;
    }
  }

  public async Task<IAresDevice?> Create(DeviceConfig config) 
    => await Load(config.UniqueId, config);
  

  public async Task<IAresDevice?> Load(string deviceId, DeviceConfig config)
  {
    try
    {
      var driver = _driverProvider.GetDriverById(config.DriverId);

      if(driver is null)
        driver = await HandleMissingDriver(config);
      

      if(driver is null)
      {
        _logger.LogError($"Failed to initialize a stored device. Tried loading a driver for {config.DeviceName}, but no suitable driver was found.");
        return null;
      }

      // Create logger
      var logger = _loggerFactory.CreateLogger(typeof(IAresDevice));

      var connectionInfo = new DeviceConnectionInfo()
      {
        DeviceId = deviceId,
        DeviceName = config.DeviceName,
        Simulated = config.IsSimulated,
        DeviceSettings = config.DeviceSettings,
        SerialConnectionInfo = config.SerialInfo
      };

      var device = (IAresDevice)ActivatorUtilities.CreateInstance(_serviceProvider, driver.DriverType, [connectionInfo]);

      if(config.SerialInfo is not null)
      {
        var requestedPort = config.SerialInfo.PortName;
        var serialConnectionResource = new ConnectionResource(requestedPort, ConnectionType.Serial);
        var success = _resourceConnectionArbiter.TryAcquireResource(serialConnectionResource, device);

        if(!success)
          throw new InvalidOperationException($"Failed to create device, resource already in use!");
      }

      _deviceRepo.AddOrUpdate(device);
      await device.Activate();

      return device;
    }

    catch(Exception e)
    {
      _logger.LogError($"Encountered an error when trying to add a device! {e.Message}");
      return null;
    }
  }

  public async Task Remove(string deviceId)
  {
    var device = _deviceRepo.GetDevice(deviceId);
    if(device != null)
    {
      device.Dispose();
      _deviceRepo.Remove(deviceId);
    }
    await Task.CompletedTask;
  }

  public async Task<IAresDevice?> Update(string deviceId, DeviceConfig config)
  {
    await Remove(deviceId);
    return await Load(deviceId, config);
  }

  private async Task<DeviceDriver?> HandleMissingDriver(DeviceConfig config)
  {
    var archivedDrivers = await _driverDatabaseManager.GetAllDrivers();
    var currentDrivers = _driverProvider.GetAllDeviceDrivers();
    var matchingArchivedDriver = archivedDrivers.FirstOrDefault(d => d.DriverId == config.DriverId);

    //This means we knew of the old driver, and should search to see if a potential replacement exists
    if(matchingArchivedDriver is not null)
    {
      var currentMatch = currentDrivers.FirstOrDefault(cd => cd.Manifest.DeviceTypeName == matchingArchivedDriver.DisplayName);

      //We successfully found a current driver matching the archived one, load it instead.
      if(currentMatch is not null)
      {
        config.DriverId = currentMatch.UniqueId;
        await _configManager.Update(config.UniqueId, config);
        return currentMatch;
      }

      //No matching driver is present
      else
      {
        var noNewDriverMessage = $"ARES detected the driver for {config.DeviceName} was deleted, an archived driver was found, but ARES could not find a new driver for the device." +
          $"To avoid the presence of ghost devices, ARES has deleted this device from your system.";
        _logger.LogWarning(noNewDriverMessage);
        await _notificationHandler.HandleNotification("Device Automatically Deleted", noNewDriverMessage, NotificationSeverityEnum.Warning);

        await _configManager.Remove(config.UniqueId);
        return null;
      }
    }

    var message = $"ARES detected the driver for {config.DeviceName} was deleted, but no reference of this devices driver was found in the driver archive." +
    $"To avoid the presence of ghost devices, ARES has deleted this device from your system.";
    _logger.LogWarning(message);
    await _notificationHandler.HandleNotification("Device Automatically Deleted", message, NotificationSeverityEnum.Warning);
    await _configManager.Remove(config.UniqueId);
    return null;
  }

  public IReadOnlyCollection<T> GetAll<T>() where T : IAresDevice => _deviceRepo.GetAll<T>();

  public T? GetDevice<T>(string id) where T : class, IAresDevice => _deviceRepo.GetDevice<T>(id);

}