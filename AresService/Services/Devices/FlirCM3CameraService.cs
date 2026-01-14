using System;
using System.Linq;
using System.Threading.Tasks;
using Ares.Core.Device;
using AresService.DeviceManagers;
using FlirCM3;
using FlirCM3.Config;
using FlirCM3.Services;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace AresService.Services.Devices;

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

  private IFlirCM3Camera GetCamera(string id)
  {
    var camera = _deviceCommandInterpreterRepo
      .GetAresDevices<IFlirCM3Camera>()
      .FirstOrDefault(cam => cam.UniqueId == id);

    if(camera is null)
      throw new InvalidOperationException($"Could not find Flir CM3 Camera {id}");

    return camera;
  }

  public override async Task<CaptureImageResponse> CaptureImage(CaptureImageRequest request, ServerCallContext context)
  {
    var camera = GetCamera(request.CameraId);
    return await camera.CaptureImage(request.SavePath);
  }

  public override async Task<GetDisplayImageResponse> GetDisplayImage(GetDisplayImageRequest request, ServerCallContext context)
  {
    var camera = GetCamera(request.CameraId);
    return new GetDisplayImageResponse() { DisplayImageData = ByteString.CopyFrom(camera.DisplayImageData) };
  }

  public override Task<GetImageResponse> GetImage(GetImageRequest request, ServerCallContext context)
  {
    var camera = GetCamera(request.CameraId);
    return Task.FromResult(new GetImageResponse() { ImageData = ByteString.CopyFrom(camera.ImageData) });
  }

  public override Task<GetImagePathResponse> GetLatestImagePath(GetImagePathRequest request, ServerCallContext context)
  {
    var camera = GetCamera(request.CameraId);
    return Task.FromResult(new GetImagePathResponse() { ImagePath = camera.LatestImagePath });
  }

  public override Task<Empty> SetExposureTime(SetExposureTimeRequest request, ServerCallContext context)
  {
    var camera = GetCamera(request.CameraId);
    camera.SetExposureTime(request.ExposureTime);

    return Task.FromResult(new Empty());
  }

  public override async Task<Empty> AddCM3Camera(FlirCM3Config cameraConfig, ServerCallContext context)
  {
    var camera = await _cameraManager.Create(cameraConfig);
    camera.PopulateSettings(cameraConfig);
    await _configManager.Add(camera.UniqueId, camera.Name, cameraConfig);
    return new Empty();
  }

  public override async Task<Empty> RemoveCM3Camera(RemoveCameraRequest request, ServerCallContext context)
  {
    await _cameraManager.Remove(request.DeviceId);
    await _configManager.Remove(request.DeviceId);
    return new Empty();
  }

  public override async Task<Empty> UpdateCM3Camera(UpdateCameraRequest updateRequest, ServerCallContext context)
  {
    var camera = await _cameraManager.Load(updateRequest.CameraId, updateRequest.Config);
    camera.PopulateSettings(updateRequest.Config);
    await _configManager.Update(updateRequest.CameraId, updateRequest.Config);
    return new Empty();
  }

  public override Task<GetAllCamerasResponse> GetAllCM3Cameras(Empty request, ServerCallContext context)
  {
    var response = new GetAllCamerasResponse();

    try
    {
      var cameraDescriptions = _deviceCommandInterpreterRepo
      .GetAresDevices<IFlirCM3Camera>()
      .Select(device => new CameraDescription { Id = device.UniqueId, Name = device.Name });

      response.Cameras.AddRange(cameraDescriptions);

      return Task.FromResult(response);
    }
    catch(Exception)
    {
      return Task.FromResult(response);
    }
  }
}
