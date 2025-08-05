using Ares.Core.Device;
using AresService.DeviceManagers;
using FlirCM3;
using FlirCM3.Config;
using FlirCM3.Services;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AresService.Services.Devices
{
  public class FlirCM3CameraService : FlirCM3CameraRpc.FlirCM3CameraRpcBase
  {
    private readonly IDeviceCommandInterpreterRepo _deviceCommandInterpreterRepo;
    private readonly IDeviceManager<FlirCM3Config, IFlirCM3Camera> _cameraManager;
    private readonly IDeviceConfigManager<FlirCM3Config> _configManager;

    public FlirCM3CameraService(IDeviceCommandInterpreterRepo deviceCommandInterpreterRepo,
      IDeviceManager<FlirCM3Config, IFlirCM3Camera> cameraManager,
      IDeviceConfigManager<FlirCM3Config> configManager)
    {
      _deviceCommandInterpreterRepo = deviceCommandInterpreterRepo;
      _cameraManager = cameraManager;
      _configManager = configManager;
    }

    private IFlirCM3Camera GetCamera(string name)
    {
      var camera = _deviceCommandInterpreterRepo
        .Select(interpreter => interpreter.Device)
        .OfType<IFlirCM3Camera>()
        .FirstOrDefault();

      if(camera is null)
        throw new InvalidOperationException($"Could not find Flir CM3 Camera {name}");

      return camera;
    }

    public override async Task<CaptureImageResponse> CaptureImage(CaptureImageRequest request, ServerCallContext context)
    {
      var camera = GetCamera(request.CameraName);
      return await camera.CaptureImage(request.SavePath);
    }

    public override Task<Empty> SetExposureTime(SetExposureTimeRequest request, ServerCallContext context)
    {
      var camera = GetCamera(request.CameraName);

      //Do things with the camera here
      camera.SetExposureTime(request.ExposureTime);

      return Task.FromResult(new Empty());
    }

    public override async Task<Empty> AddCM3Camera(FlirCM3Config cameraConfig, ServerCallContext context)
    {
      await _cameraManager.Load(cameraConfig);
      var camera = GetCamera(cameraConfig.Name);
      camera.PopulateSettings(cameraConfig);
      await _configManager.Add(cameraConfig.Name, cameraConfig);
      return new Empty();
    }

    public override async Task<Empty> RemoveCM3Camera(RemoveCameraRequest request, ServerCallContext context)
    {
      await _cameraManager.Remove(request.DeviceName);
      await _configManager.Remove(request.DeviceName);
      return new Empty();
    }

    public override async Task<Empty> UpdateCM3Camera(FlirCM3Config cameraConfig, ServerCallContext context)
    {
      var camera = await _cameraManager.Load(cameraConfig);
      camera.PopulateSettings(cameraConfig);
      await _configManager.Update(cameraConfig.Name, cameraConfig);
      return new Empty();
    }

    public override Task<GetAllCamerasResponse> GetAllCM3Cameras(Empty request, ServerCallContext context)
    {
      var deviceNames = _deviceCommandInterpreterRepo
        .Select(deviceInterpreter => deviceInterpreter.Device)
        .OfType<IFlirCM3Camera>()
        .Select(device => device.Name);

      var response = new GetAllCamerasResponse();
      response.CameraIds.AddRange(deviceNames);
      response.DeviceNames.AddRange(deviceNames);
      return Task.FromResult(response);
    }
  }
}
