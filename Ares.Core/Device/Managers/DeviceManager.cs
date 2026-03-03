using Ares.Core.Device.Providers;
using Ares.Core.Device.Repos;
using Ares.Core.Resources;
using Ares.Datamodel.Device;
using Ares.Device;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Ares.Core.Device.Managers;

public class DeviceManager : IDeviceManager
{
  private readonly IAresDriverProvider _driverProvider;
  private readonly IAresDeviceRepo _deviceRepo;
  private readonly IServiceProvider _serviceProvider;
  private readonly ILoggerFactory _loggerFactory;
  private readonly IDbContextFactory<CoreDatabaseContext> _dbContextFactory;
  private readonly ILogger<DeviceManager> _logger;
  private readonly IResourceConnectionArbiter _resourceConnectionArbiter;

  public DeviceManager(
    IAresDriverProvider driverProvider,
    IAresDeviceRepo deviceRepository,
    IServiceProvider serviceProvider,
    ILoggerFactory loggerFactory,
    IDbContextFactory<CoreDatabaseContext> dbContextFactory,
    IResourceConnectionArbiter resourceConnectionArbiter)
  {
    _driverProvider = driverProvider;
    _deviceRepo = deviceRepository;
    _serviceProvider = serviceProvider;
    _loggerFactory = loggerFactory;
    _dbContextFactory = dbContextFactory;
    _logger = loggerFactory.CreateLogger<DeviceManager>();
    _resourceConnectionArbiter = resourceConnectionArbiter;
  }

  public async Task<IAresDevice> Create(DeviceConfig config)
  {
    var deviceId = Guid.NewGuid().ToString();
    return await Load(deviceId, config);
  }

  public async Task<IAresDevice> Load(string deviceId, DeviceConfig config)
  {
    try
    {
      var driver = _driverProvider.GetDriverById(config.DriverId);
      if(driver == null)
      {
        throw new InvalidOperationException($"Driver '{config.DriverName}' not found.");
      }

      // Create logger
      var logger = _loggerFactory.CreateLogger(typeof(IAresDevice));

      IAresDevice device;

      if(driver.ConnectionType == ConnectionType.Serial)
        device = (IAresDevice)ActivatorUtilities.CreateInstance(_serviceProvider, driver.DriverType, [config.DeviceName, config.Serial, config.DriverSettings]);

      else
        device = (IAresDevice)ActivatorUtilities.CreateInstance(_serviceProvider, driver.DriverType, [config.DeviceName, config.DriverSettings, logger]);

      switch(config.TransportCase)
      {
        case DeviceConfig.TransportOneofCase.Serial:
          var requestedPort = config.Serial.PortName;
          var serialConnectionResource = new ConnectionResource(requestedPort, ConnectionType.Serial);
          var success = _resourceConnectionArbiter.TryAcquireResource(serialConnectionResource, device);

          if(!success)
            throw new InvalidOperationException($"Failed to create device, resource already in use!");

          break;
      }

      _deviceRepo.AddOrUpdate(device);
      await device.Activate();

      return device;
    }

    catch(Exception e)
    {
      _logger.LogError($"Encountered an error when trying to add a device! {e.Message}");
      throw;
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
        _logger.LogError(ex, "Error loading device {DeviceName} with driver {DriverName}", config.DeviceName, config.DriverName);
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

  public async Task<IAresDevice> Update(string deviceId, DeviceConfig config)
  {
    await Remove(deviceId);
    return await Load(deviceId, config);
  }

  public async Task LoadDevices()
  {
    using var context = await _dbContextFactory.CreateDbContextAsync();
    var configs = await context.DeviceConfigs.ToListAsync();
    await Load(configs);
  }

  public IReadOnlyCollection<T> GetAll<T>() where T : IAresDevice => _deviceRepo.GetAll<T>();

  public T? GetDevice<T>(string id) where T : class, IAresDevice => _deviceRepo.GetDevice<T>(id);

}