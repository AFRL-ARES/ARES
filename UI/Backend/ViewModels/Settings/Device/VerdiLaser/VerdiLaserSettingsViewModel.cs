using Ares.Datamodel.Device;
using Ares.Services.Device;
using CommunityToolkit.Mvvm.Messaging;
using Grpc.Core;
using ReactiveUI;
using UI.Backend.Devices;
using VerdiV6.Config;
using VerdiV6.Services;

namespace UI.Backend.ViewModels.Settings.Device.VerdiLaser
{
  public class VerdiLaserSettingsViewModel : ReactiveObject
  {
    private readonly VerdiV6Rpc.VerdiV6RpcClient _laserClient;
    private readonly DeviceConfig _deviceConfig;
    private readonly AresDevices.AresDevicesClient _devicesClient;
    private readonly IMessenger _messenger;

    public VerdiLaserSettingsViewModel(DeviceConfig deviceConfig,
      VerdiV6Rpc.VerdiV6RpcClient laserClient,
      AresDevices.AresDevicesClient devicesClient,
      IMessenger messenger,
      Func<Task> onRemoveCallback)
    {
      _deviceConfig = deviceConfig;
      _laserClient = laserClient;
      LaserConfig = deviceConfig.ConfigData.Unpack<VerdiConfig>();
      _devicesClient = devicesClient;
      _messenger = messenger;
      OnRemoveCallback = onRemoveCallback;
      EditViewModel = new VerdiLaserConfigEditViewModel(_laserClient, _devicesClient, LaserConfig);
    }

    public VerdiConfig LaserConfig { get; }
    public Func<Task> OnRemoveCallback { get; }
    public VerdiLaserConfigEditViewModel EditViewModel { get; }

    public Task<DeviceOperationalStatus> GetDeviceOperationalStatus()
    {
      try
      {
        return _devicesClient.GetDeviceStatusAsync(new DeviceStatusRequest { DeviceId = _deviceConfig.UniqueId }).ResponseAsync;
      }

      catch(RpcException)
      {
        return Task.FromResult(new DeviceOperationalStatus { OperationalState = OperationalState.Error, Message = $"Unable to find a registered V6 Laser with a name {LaserConfig.Name}" });
      }
    }

    public async Task Save()
    {
      var laserConfig = EditViewModel.Save();
      var updateRequest = new LaserUpdateRequest
      {
        Id = _deviceConfig.UniqueId,
        Config = laserConfig
      };
      await _laserClient.UpdateLaserAsync(updateRequest);
    }

    public Task Activate()
      => _devicesClient.ActivateAsync(new DeviceActivateRequest
      {
        DeviceId = _deviceConfig.UniqueId
      }).ResponseAsync;

    public async Task Remove()
    {
      await _laserClient.RemoveLaserAsync(new DeviceRequest { DeviceId = _deviceConfig.UniqueId });
      _messenger.Send(new DeviceDeletedMessage(_deviceConfig.UniqueId));
      await OnRemoveCallback();
    }

  }
}
