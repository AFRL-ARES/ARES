using Ares.Services.Device;
using DynamicData;
using FlirCM3.Services;
using Google.Protobuf.WellKnownTypes;
using UI.Backend.Factories;
using UI.Infrastructure.Repos;
using UI.Backend.ViewModels;
using UI.Backend.ViewModels.Devices.CM3Camera;

namespace UI.Features.Devices.CM3Camera;

public class CM3CamDeviceControlViewModelFactory : DeviceConnectorViewModelFactory<CM3CameraUnitControlViewModel>
{
  private readonly FlirCM3CameraRpc.FlirCM3CameraRpcClient _client;
  private IDeviceControlViewModelRepo _deviceControlViewModelRepo;

  public CM3CamDeviceControlViewModelFactory(AresDevices.AresDevicesClient devicesClient,
    FlirCM3CameraRpc.FlirCM3CameraRpcClient cameraClient,
    IDeviceControlViewModelRepo deviceControlViewModelRepo) : base(devicesClient, deviceControlViewModelRepo)
  {
    _client = cameraClient;
    _deviceControlViewModelRepo = deviceControlViewModelRepo;
  }

  protected override void CreateAndAddViewModel(string deviceId, string deviceName)
    => _deviceControlViewModelRepo.Add(new CM3CameraUnitControlViewModel(deviceId, deviceName, _client));

  protected override async Task<IEnumerable<AresDeviceDescription>> GetAvailableDevices()
  {
    var devInfos = await _client.GetAllCM3CamerasAsync(new Empty());
    var descriptions = devInfos.Cameras.Select(dev => new AresDeviceDescription(dev.Id, dev.Name)).ToArray();
    return descriptions;
  }
}
