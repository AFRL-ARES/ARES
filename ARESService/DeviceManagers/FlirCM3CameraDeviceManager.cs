using Ares.Core.Device;
using FlirCM3;
using FlirCM3.Config;
using FlirCM3.Simulation;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AresService.DeviceManagers
{
  public class FlirCM3CameraDeviceManager : IDeviceManager<FlirCM3Config, IFlirCM3Camera>
  {
    private readonly IDeviceCommandInterpreterRepo _deviceCommandInterpreterRepo;

    public FlirCM3CameraDeviceManager(IDeviceCommandInterpreterRepo deviceCommandInterpreterRepo)
    {
      _deviceCommandInterpreterRepo = deviceCommandInterpreterRepo;
    }

    public async Task<IFlirCM3Camera> Load(FlirCM3Config config)
    {
      IFlirCM3Camera camera;

      if(config.Simulated)
        camera = new SimCM3Camera(config.Name);

      else
        camera = new FlirCM3Camera(config.Name);

      await camera.Activate();
      camera.PopulateSettings(config);
      var interepreter = new FlirCM3CameraInterpreter(camera);
      _deviceCommandInterpreterRepo.Add(interepreter);

      return camera;
    }

    public async Task<IEnumerable<IFlirCM3Camera>> Load(IEnumerable<FlirCM3Config> configs)
    {
      var cameras = await Task.WhenAll(configs.Select(Load));
      return cameras;
    }

    public async Task Remove(string managerName)
    {
      var cameraInterpreter = _deviceCommandInterpreterRepo
    .FirstOrDefault(interpreter => interpreter.Device.Name == managerName);

      if(cameraInterpreter?.Device is not IFlirCM3Camera cm3Camera)
        return;

      await cm3Camera.DisposeAsync();
      _deviceCommandInterpreterRepo.Remove(cameraInterpreter);
    }

    public async Task<IFlirCM3Camera> Update(FlirCM3Config config)
    {
      var existingCamera = _deviceCommandInterpreterRepo
      .Select(interpreter => interpreter.Device)
      .OfType<IFlirCM3Camera>()
      .FirstOrDefault(device => device.Name == config.Name);

      if(existingCamera is null)
        return await Load(config);

      // if nothing changed, don't bother re-adding the device
      if(existingCamera.Name == config.Name)
        return existingCamera;

      await Remove(existingCamera.Name);

      return await Load(config);
    }
  }
}
