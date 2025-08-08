using Ares.Datamodel.Device;
using Ares.Services.Device;
using FlirCM3.Config;
using FlirCM3.Services;
using Grpc.Core;
using ReactiveUI;

namespace UI.Backend.ViewModels.Settings.Device.CM3Camera
{
  public class CM3CameraSettingsViewModel : ReactiveObject
  {
    private readonly FlirCM3CameraRpc.FlirCM3CameraRpcClient _cameraClient;
    private readonly DeviceConfig _deviceConfig;
    private readonly AresDevices.AresDevicesClient _devicesClient;

    public CM3CameraSettingsViewModel(DeviceConfig deviceConfig,
      FlirCM3CameraRpc.FlirCM3CameraRpcClient cameraClient,
      AresDevices.AresDevicesClient devicesClient,
      Func<Task> onRemoveCallBack)
    {
      _cameraClient = cameraClient;
      _deviceConfig = deviceConfig;
      _devicesClient = devicesClient;
      FlirCM3Config = deviceConfig.ConfigData.Unpack<FlirCM3Config>();
      OnRemoveCallback = onRemoveCallBack;
      EditViewModel = new FlirCM3ConfigEditViewModel(_cameraClient, _devicesClient, FlirCM3Config);
    }

    public Task<DeviceStatus> GetDeviceStatus()
    {
      try
      {
        return _devicesClient.GetDeviceStatusAsync(new DeviceStatusRequest { DeviceName = FlirCM3Config.Name }).ResponseAsync;
      }

      catch(RpcException)
      {
        return Task.FromResult(new DeviceStatus { DeviceState = DeviceState.Error, Message = $"Unable to find a registered Flir CM3 Camera with a name {FlirCM3Config.Name}" });
      }
    }

    public async Task Save()
    {
      var cameraConfig = EditViewModel.Save();
      await _cameraClient.UpdateCM3CameraAsync(cameraConfig);
    }

    public Task Activate()
      => _devicesClient.ActivateAsync(new DeviceActivateRequest
      {
        DeviceName = FlirCM3Config.Name
      }).ResponseAsync;

    public async Task Remove()
    {
      await _cameraClient.RemoveCM3CameraAsync(new RemoveCameraRequest { DeviceName = FlirCM3Config.Name });
    }


    public FlirCM3Config FlirCM3Config { get; }

    public Func<Task> OnRemoveCallback { get; }

    public FlirCM3ConfigEditViewModel EditViewModel { get; }

  }
}
