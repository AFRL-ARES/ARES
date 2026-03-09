using Ares.Core.Device.Providers;
using Ares.Core.Device.Repos;
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
  private readonly IAresDeviceRepo _deviceRepo;
  private readonly IServiceProvider _serviceProvider;
  private readonly ILoggerFactory _loggerFactory;
  private readonly ILogger<DeviceManager> _logger;
  private readonly IResourceConnectionArbiter _resourceConnectionArbiter;
  private readonly CompositeDisposable _cleanup = new();

  public DeviceManager(
    IDeviceDriverProvider driverProvider,
    IAresDeviceRepo deviceRepository,
    IDeviceConfigProvider configProvider,
    IServiceProvider serviceProvider,
    ILoggerFactory loggerFactory,
    IResourceConnectionArbiter resourceConnectionArbiter)
  {
    _driverProvider = driverProvider;
    _deviceRepo = deviceRepository;
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

      //if(driver == null)
      //  throw new InvalidOperationException($"Driver for '{config.DeviceName}' not found.");
      
      // Create logger
      var logger = _loggerFactory.CreateLogger(typeof(IAresDevice));

      IAresDevice device;

      if(driver.ConnectionType == ConnectionType.Serial)
        device = (IAresDevice)ActivatorUtilities.CreateInstance(_serviceProvider, driver.DriverType, [config.DeviceName, config.DeviceId, config.SerialInfo, config.DeviceSettings]);

      else
        device = (IAresDevice)ActivatorUtilities.CreateInstance(_serviceProvider, driver.DriverType, [config.DeviceName, config.DeviceId, config.DeviceSettings, logger]);

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

  public async Task<IAresDevice[]> Load(IEnumerable<DeviceConfig> configs)
  {
    var devices = new List<IAresDevice>();
    foreach(var config in configs)
    {
      try
      {
        var deviceId = string.IsNullOrEmpty(config.UniqueId) ? Guid.NewGuid().ToString() : config.UniqueId;
        var device = await Load(deviceId, config);
        devices.Add(device);
      }
      catch(Exception ex)
      {
        _logger.LogError(ex, "Error loading device {DeviceName} with driver.", config.DeviceName);
      }
    }
    return devices.ToArray();
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

  public async Task LoadDevices()
  {
    var configs = _configProvider.GetAllConfigs();
    await Load(configs);
  }

  public IReadOnlyCollection<T> GetAll<T>() where T : IAresDevice => _deviceRepo.GetAll<T>();

  public T? GetDevice<T>(string id) where T : class, IAresDevice => _deviceRepo.GetDevice<T>(id);

}