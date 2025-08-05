using Ares.Messaging.Device;
using Grpc.Core;
using ReactiveUI;
using VerdiV6.Config;
using VerdiV6.Services;

namespace UI.Backend.ViewModels.Settings.Device.VerdiLaser
{
  public class VerdiLaserSettingsViewModel : ReactiveObject
  {
    private readonly VerdiV6Rpc.VerdiV6RpcClient _laserClient;
    private readonly DeviceConfig _deviceConfig;
    private readonly AresDevices.AresDevicesClient _devicesClient;

    public VerdiLaserSettingsViewModel(DeviceConfig deviceConfig,
      VerdiV6Rpc.VerdiV6RpcClient laserClient,
      AresDevices.AresDevicesClient devicesClient,
      Func<Task> onRemoveCallback)
    {
      _deviceConfig = deviceConfig;
      _laserClient = laserClient;
      LaserConfig = deviceConfig.ConfigData.Unpack<VerdiConfig>();
      _devicesClient = devicesClient;
      OnRemoveCallback = onRemoveCallback;
      EditViewModel = new VerdiLaserConfigEditViewModel(_laserClient, _devicesClient, LaserConfig);
    }

    public VerdiConfig LaserConfig { get; }
    public Func<Task> OnRemoveCallback { get; }
    public VerdiLaserConfigEditViewModel EditViewModel { get; }

    public Task<DeviceStatus> GetDeviceStatus()
    {
      try
      {
        return _devicesClient.GetDeviceStatusAsync(new DeviceStatusRequest { DeviceName = LaserConfig.Name }).ResponseAsync;
      }

      catch(RpcException)
      {
        return Task.FromResult(new DeviceStatus { DeviceState = DeviceState.Error, Message = $"Unable to find a registered V6 Laser with a name {LaserConfig.Name}" });
      }
    }

    public async Task Save()
    {
      var laserConfig = EditViewModel.Save();
      await _laserClient.UpdateLaserAsync(laserConfig);
    }

    public Task Activate()
      => _devicesClient.ActivateAsync(new DeviceActivateRequest
      {
        DeviceName = LaserConfig.Name
      }).ResponseAsync;

    public async Task Remove()
    {
      await _laserClient.RemoveLaserAsync(new DeviceRequest { DeviceName = _deviceConfig.DeviceName });
      await OnRemoveCallback();
    }

  }
}
