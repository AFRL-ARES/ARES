using Ares.Messaging.Device;
using Grpc.Core;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using TubeFurnace.Config;
using TubeFurnace.Messaging;

namespace UI.Backend.ViewModels.Settings.Device.TubeFurnace
{
  public class TubeFurnaceSettingsViewModel : ReactiveObject
  {
    private readonly DeviceConfig _deviceConfig;
    private readonly AresDevices.AresDevicesClient _devicesClient;
    private readonly TubeFurnaceRpc.TubeFurnaceRpcClient _tubeFurnaceClient;

    public TubeFurnaceSettingsViewModel(DeviceConfig deviceConfig,
      TubeFurnaceRpc.TubeFurnaceRpcClient tubeFurnaceClient,
      AresDevices.AresDevicesClient devicesClient,
      Func<Task> onRemoveCallback)
    {
      _deviceConfig = deviceConfig;
      TubeFurnaceConfig = deviceConfig.ConfigData.Unpack<TubeFurnaceConfig>();
      _tubeFurnaceClient = tubeFurnaceClient;
      _devicesClient = devicesClient;
      OnRemoveCallback = onRemoveCallback;
      EditViewModel = new TubeFurnaceConfigEditViewModel(_tubeFurnaceClient, _devicesClient, TubeFurnaceConfig);
    }

    public TubeFurnaceConfig TubeFurnaceConfig { get; }

    public Func<Task> OnRemoveCallback { get; }

    public TubeFurnaceConfigEditViewModel EditViewModel { get; }

    public async Task<DeviceStatus> GetDeviceStatus()
    {
      try
      {
        var status = await _devicesClient.GetDeviceStatusAsync(new DeviceStatusRequest { DeviceName = TubeFurnaceConfig.Name }).ResponseAsync;
        DeviceActive = status.DeviceState is DeviceState.Active;
        return status;
      }
      catch (RpcException)
      {
        return new DeviceStatus { DeviceState = DeviceState.Error, Message = $"Unable to find a registered stepper controller with a name {TubeFurnaceConfig.Name}" };
      }
    }

    public Task Activate()
      => _devicesClient.ActivateAsync(new DeviceActivateRequest { DeviceName = TubeFurnaceConfig.Name }).ResponseAsync;

    public async Task Save()
    {
      var tubeFurnaceConfig = EditViewModel.Save();
      await _tubeFurnaceClient.UpdateTubeFurnaceAsync(tubeFurnaceConfig);
    }

    public async Task Remove()
    {
      await _tubeFurnaceClient.RemoveTubeFurnaceAsync(new TubeFurnaceRequest { TubeFurnaceName = _deviceConfig.DeviceName });
      await OnRemoveCallback();
    }

    public async Task Init()
    {
      var status = await GetDeviceStatus();
      if (status.DeviceState != DeviceState.Active)
        return;

      var deviceState = await _tubeFurnaceClient.GetStateAsync(new TubeFurnaceRequest { TubeFurnaceName = _deviceConfig.DeviceName });

    }

    [Reactive]
    public bool DeviceActive { get; private set; }
  }
}