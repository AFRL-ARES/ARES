using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ares.Core.Device;
using AresService.DeviceDbLoaders;
using FlirCM3;
using FlirCM3.Config;
using FlirCM3.Simulation;

namespace AresService.DeviceManagers;

public class FlirCM3CameraDeviceManager : IDeviceManager<FlirCM3Config, IFlirCM3Camera>
{
  private readonly IDeviceCommandInterpreterRepo _deviceCommandInterpreterRepo;

  public FlirCM3CameraDeviceManager(IDeviceCommandInterpreterRepo deviceCommandInterpreterRepo)
  {
    _deviceCommandInterpreterRepo = deviceCommandInterpreterRepo;
  }

  public Task<IFlirCM3Camera> Create(FlirCM3Config config)
  {
    return Load(Guid.NewGuid().ToString(), config);
  }

  public async Task<IFlirCM3Camera> Load(string id, FlirCM3Config config)
  {
    IFlirCM3Camera camera;

    if(config.Simulated)
      camera = new SimCM3Camera(config.Name)
      {
        UniqueId = id
      };

    else
      camera = new FlirCM3Camera(config.Name)
      {
        UniqueId = id
      };

    await camera.Activate();
    camera.PopulateSettings(config);
    var interepreter = new FlirCM3CameraInterpreter(camera);
    _deviceCommandInterpreterRepo.Add(interepreter);

    return camera;
  }

  public async Task<IFlirCM3Camera[]> Load(IEnumerable<LoadableConfig<FlirCM3Config>> loadableConfigs)
  {
    var cameras = await Task.WhenAll(loadableConfigs.Select(cfg => Load(cfg.Id, cfg.DeviceConfig)));
    return cameras;
  }

  public async Task Remove(string managerId)
  {
    var cm3Camera = _deviceCommandInterpreterRepo
      .GetAresDevice<IFlirCM3Camera>(managerId);

    if(cm3Camera is null)
      return;

    await cm3Camera.DisposeAsync();
    _deviceCommandInterpreterRepo.Remove(cm3Camera.UniqueId);
  }

  public async Task<IFlirCM3Camera> Update(string deviceId, FlirCM3Config config)
  {
    var existingCamera = _deviceCommandInterpreterRepo
    .GetAresDevice<IFlirCM3Camera>(deviceId);

    if(existingCamera is null)
      return await Create(config);

    // if nothing changed, don't bother re-adding the device
    if(existingCamera.Name == config.Name)
      return existingCamera;

    await Remove(existingCamera.UniqueId);

    return await Load(existingCamera.UniqueId, config);
  }
}
