using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ares.Core.Device;
using AresService.DeviceDbLoaders;
using RestDevice;
using RestDevice.Config;

namespace AresService.DeviceManagers;

public class RestDeviceManager : IDeviceManager<RestDeviceConfig, IRestDevice>
{
  private readonly IDeviceCommandInterpreterRepo _deviceCommandInterpreterRepo;
  public RestDeviceManager(IDeviceCommandInterpreterRepo deviceCommandInterpreterRepo)
  {
    _deviceCommandInterpreterRepo = deviceCommandInterpreterRepo;
  }

  public Task<IRestDevice> Create(RestDeviceConfig config)
  {
    return Load(Guid.NewGuid().ToString(), config);
  }

  public async Task<IRestDevice> Load(string id, RestDeviceConfig config)
  {
    IRestDevice restDevice;
    if(config.Simulated)
      //TODO: Fix this
      restDevice = new RestDevice.RestDevice(config.Name, config.Address)
      {
        UniqueId = id,
      };

    else
      restDevice = new RestDevice.RestDevice(config.Name, config.Address)
      {
        UniqueId = id
      };

    await restDevice.Activate();
    var interpreter = new RestDeviceInterpreter(restDevice);
    _deviceCommandInterpreterRepo.Add(interpreter);

    return restDevice;
  }

  public async Task<IRestDevice[]> Load(IEnumerable<LoadableConfig<RestDeviceConfig>> configs)
  {
    var restDevices = await Task.WhenAll(configs.Select(cfg => Load(cfg.Id, cfg.DeviceConfig)));
    return restDevices;
  }

  public async Task Remove(string managerId)
  {
    var deviceInterpreter = _deviceCommandInterpreterRepo.FirstOrDefault(interpreter => interpreter.Device.UniqueId == managerId);

    if(deviceInterpreter?.Device is not IRestDevice restDevice)
      return;

    await restDevice.DisposeAsync();
    _deviceCommandInterpreterRepo.Remove(deviceInterpreter);
  }

  public async Task<IRestDevice> Update(string id, RestDeviceConfig config)
  {
    var existingCameraManager = _deviceCommandInterpreterRepo
      .Select(interpreter => interpreter.Device)
      .OfType<IRestDevice>()
      .FirstOrDefault(device => device.UniqueId == id);

    if(existingCameraManager is null)
      return await Create(config);

    // if nothing changed, don't bother re-adding the device
    if(existingCameraManager.Name == config.Name)
      return existingCameraManager;

    await Remove(existingCameraManager.UniqueId);

    return await Load(id, config);
  }
}
