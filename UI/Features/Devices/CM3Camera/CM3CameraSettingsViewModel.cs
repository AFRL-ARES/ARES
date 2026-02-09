using Ares.Datamodel.Device;
using Ares.Services.Device;
using CommunityToolkit.Mvvm.Messaging;
using FlirCM3.Config;
using FlirCM3.Services;
using Grpc.Core;
using ReactiveUI;
using UI.Features.Devices.Shared;

namespace UI.Features.Devices.CM3Camera;

public class CM3CameraSettingsViewModel : ReactiveObject
{
  private readonly FlirCM3CameraRpc.FlirCM3CameraRpcClient _cameraClient;
  private readonly DeviceConfig _deviceConfig;
  private readonly AresDevices.AresDevicesClient _devicesClient;
  private readonly IMessenger _deviceDeletionMessenger;

  public CM3CameraSettingsViewModel(DeviceConfig deviceConfig,
    FlirCM3CameraRpc.FlirCM3CameraRpcClient cameraClient,
    AresDevices.AresDevicesClient devicesClient,
    IMessenger deviceDeletionMessenger,
    Func<Task> onRemoveCallBack)
  {
    _cameraClient = cameraClient;
    _deviceConfig = deviceConfig;
    _devicesClient = devicesClient;
    _deviceDeletionMessenger = deviceDeletionMessenger;
    FlirCM3Config = deviceConfig.ConfigData.Unpack<FlirCM3Config>();
    OnRemoveCallback = onRemoveCallBack;
    EditViewModel = new FlirCM3ConfigEditViewModel(_cameraClient, _devicesClient, FlirCM3Config);
  }

  public Task<DeviceOperationalStatus> GetDeviceOperationalStatus()
  {
    try
    {
      return _devicesClient.GetDeviceStatusAsync(new DeviceStatusRequest { DeviceId = _deviceConfig.UniqueId }).ResponseAsync;
    }

    catch(RpcException)
    {
      return Task.FromResult(new DeviceOperationalStatus { OperationalState = OperationalState.Error, Message = $"Unable to find a registered Flir CM3 Camera with a name {FlirCM3Config.Name}" });
    }
  }

  public async Task Save()
  {
    var cameraConfig = EditViewModel.Save();
    var updateRequest = new UpdateCameraRequest
    {
      CameraId = cameraConfig.Id,
      Config = cameraConfig
    };

    await _cameraClient.UpdateCM3CameraAsync(updateRequest);
  }

  public Task Activate()
    => _devicesClient.ActivateAsync(new DeviceActivateRequest
    {
      DeviceId = _deviceConfig.UniqueId
    }).ResponseAsync;

  public async Task Remove()
  {
    await _cameraClient.RemoveCM3CameraAsync(new RemoveCameraRequest { DeviceId = _deviceConfig.UniqueId });
    _deviceDeletionMessenger.Send(new DeviceDeletedMessage(_deviceConfig.UniqueId));
    await OnRemoveCallback();
  }

  public FlirCM3Config FlirCM3Config { get; }

  public Func<Task> OnRemoveCallback { get; }

  public FlirCM3ConfigEditViewModel EditViewModel { get; }

}
