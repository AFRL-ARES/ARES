using Ares.Core.Device.Repos;
using Ares.Datamodel.Device;
using Ares.Device;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Ares.Core.Device.Managers;

public class DeviceManager : IDeviceManager
{
  private readonly IDeviceDriverRepo _driverRepo;
  private readonly IAresDeviceRepo _deviceRepo;
  private readonly IServiceProvider _serviceProvider;
  private readonly ILoggerFactory _loggerFactory;

  public DeviceManager(
    IDeviceDriverRepo driverRepository,
    IAresDeviceRepo deviceRepository,
    IServiceProvider serviceProvider,
    ILoggerFactory loggerFactory)
  {
    _driverRepo = driverRepository;
    _deviceRepo = deviceRepository;
    _serviceProvider = serviceProvider;
    _loggerFactory = loggerFactory;
  }

  public async Task<IAresDevice> Create(DeviceConfig config)
  {
    var deviceId = Guid.NewGuid().ToString();
    return await Load(deviceId, config);
  }

  public async Task<IAresDevice> Load(string deviceId, DeviceConfig config)
  {
    var driver = _driverRepo.GetByName(config.DriverName);
    if(driver == null)
    {
      throw new InvalidOperationException($"Driver '{config.DriverName}' not found.");
    }

    // Create a logger for the specific device type
    var loggerType = typeof(ILogger<>).MakeGenericType(driver.DriverType);
    var logger = _loggerFactory.CreateLogger(driver.DriverType);

    // Instantiate with: string (name), AresStruct (config), and ILogger
    // Using explicit arguments to match the requested constructor pattern
    var device = (IAresDevice)ActivatorUtilities.CreateInstance(_serviceProvider, driver.DriverType, 
      config.DeviceName, 
      config.DriverSettings,
      logger);
    
    _deviceRepo.Add(device);
    
    await device.Activate();
    
    return device;
  }

  public async Task<IAresDevice[]> Load(IEnumerable<DeviceConfig> configs)
  {
    var devices = new List<IAresDevice>();
    foreach(var config in configs)
    {
      try
      {
        var device = await Create(config);
        devices.Add(device);
      }
      catch(Exception)
      {
        // Log error loading specific device
      }
    }
    return devices.ToArray();
  }

  public async Task Remove(string deviceId)
  {
    var device = _deviceRepo.GetAresDevice(deviceId);
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
}