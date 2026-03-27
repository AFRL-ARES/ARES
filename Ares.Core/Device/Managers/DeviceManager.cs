using Ares.Core.CoreDevice;
using Ares.Core.Device.Providers;
using Ares.Core.Device.Repos;
using Ares.Core.Notifications;
using Ares.Core.Resources;
using Ares.Datamodel.Device;
using Ares.Device;
using DynamicData;
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
    INotificationHandler notificationHandler,
    IDeviceConfigProvider configProvider,
    IServiceProvider serviceProvider,
    ILoggerFactory loggerFactory,
    IResourceConnectionArbiter resourceConnectionArbiter)
  {
    _driverProvider = driverProvider;
    _deviceRepo = deviceRepository;
    _notificationHandler = notificationHandler;
    _configProvider = configProvider;
    _serviceProvider = serviceProvider;
    _loggerFactory = loggerFactory;
    _logger = loggerFactory.CreateLogger<DeviceManager>();
    _resourceConnectionArbiter = resourceConnectionArbiter;
  }

  public void Initialize()
  {
    var coreDevice = new AresCoreDevice();
    _deviceRepo.AddOrUpdate(coreDevice);

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
  

  public async Task<IAresDevice?> Load(string configId, DeviceConfig config)
  {
    try
    {
      var driver = _driverProvider.GetDriverById(config.DriverId);

      if(driver is null)
      {
        _logger.LogError($"Failed to initialize a stored device. Tried loading a driver for {config.DeviceName}, but no suitable driver was found.");
        return null;
      }

      // Create logger
      var logger = _loggerFactory.CreateLogger(typeof(IAresDevice));

      var connectionInfo = new DeviceConnectionInfo()
      {
        DeviceId = config.DeviceId,
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
        {
          var owner = _resourceConnectionArbiter.GetResourceOwner(serialConnectionResource);
          if(owner?.UniqueId == device.UniqueId)
          {
            var message = $"Failed to add device {device.Name} as the resource it tried to use ({serialConnectionResource.ResourceName}) was already in use by another device.";
            _logger.LogError(message);
            await _notificationHandler.HandleNotification("Failed to Add Device", message, NotificationSeverityEnum.Error);
            return null;
          }
        }
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
}