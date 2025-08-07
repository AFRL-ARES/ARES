using Ares.Core.Device;
using RestDevice;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RestDevice.Config;

namespace AresService.DeviceManagers;

public class RestDeviceManager : IDeviceManager<RestDeviceConfig, IRestDevice>
{
  private readonly IDeviceCommandInterpreterRepo _deviceCommandInterpreterRepo;
  public RestDeviceManager(IDeviceCommandInterpreterRepo deviceCommandInterpreterRepo)
  {
    _deviceCommandInterpreterRepo = deviceCommandInterpreterRepo;
  }

  public async Task<IRestDevice> Load(RestDeviceConfig config)
  {
    IRestDevice restDevice;

    if(config.Simulated)
      //TODO: Fix this
      restDevice = new RestDevice.RestDevice(config.Name, config.Address);

    else
      restDevice = new RestDevice.RestDevice(config.Name, config.Address);

    await restDevice.Activate();
    var interpreter = new RestDeviceInterpreter(restDevice);
    _deviceCommandInterpreterRepo.Add(interpreter);

    return restDevice;
  }

  public async Task<IEnumerable<IRestDevice>> Load(IEnumerable<RestDeviceConfig> configs)
  {
    var restDevices = await Task.WhenAll(configs.Select(Load));
    return restDevices;
  }

  public async Task Remove(string managerName)
  {
    var deviceInterpreter = _deviceCommandInterpreterRepo.FirstOrDefault(interpreter => interpreter.Device.Name == managerName);

    if(deviceInterpreter?.Device is not IRestDevice restDevice)
      return;

    await restDevice.DisposeAsync();
    _deviceCommandInterpreterRepo.Remove(deviceInterpreter);
  }

  public async Task<IRestDevice> Update(RestDeviceConfig config)
  {
    var existingCameraManager = _deviceCommandInterpreterRepo
      .Select(interpreter => interpreter.Device)
      .OfType<IRestDevice>()
      .FirstOrDefault(device => device.Name == config.Name);

    if(existingCameraManager.Name == config.Name)
      return existingCameraManager;

    await Remove(existingCameraManager.Name);

    return await Load(config);
  }
}
