using Ares.Datamodel.Device;
using Ares.Services.Device;
using Chiller.Config;
using Chiller.Services;
using Grpc.Core;
using ReactiveUI;

namespace UI.Backend.ViewModels.Settings.Device.LaserChiller;

public class LaserChillerSettingsViewModel : ReactiveObject
{
  private readonly ChillerRpc.ChillerRpcClient _chillerClient;
  private readonly DeviceConfig _deviceConfig;
  private readonly AresDevices.AresDevicesClient _devicesClient;

  public LaserChillerSettingsViewModel(DeviceConfig deviceConfig,
    ChillerRpc.ChillerRpcClient chillerClient,
    AresDevices.AresDevicesClient devicesClient,
    Func<Task> onRemoveCallback)
  {
    _deviceConfig = deviceConfig;
    _chillerClient = chillerClient;
    _devicesClient = devicesClient;
    OnRemoveCallback = onRemoveCallback;
    ChillerConfig = deviceConfig.ConfigData.Unpack<ChillerConfig>();
    EditViewModel = new LaserChillerConfigEditViewModel(_chillerClient, _devicesClient, ChillerConfig);
  }

  public Task<DeviceStatus> GetDeviceStatus()
  {
    try
    {
      return _devicesClient.GetDeviceStatusAsync(new DeviceStatusRequest { DeviceName = ChillerConfig.Name }).ResponseAsync;
    }

    catch(RpcException)
    {
      return Task.FromResult(new DeviceStatus { DeviceState = DeviceState.Error, Message = $"Unable to find a registered V6 Laser with a name {ChillerConfig.Name}" });
    }
  }

  public async Task Save()
  {
    var laserConfig = EditViewModel.Save();
    await _chillerClient.UpdateChillerAsync(laserConfig);
  }

  public Task Activate()
    => _devicesClient.ActivateAsync(new DeviceActivateRequest
    {
      DeviceName = ChillerConfig.Name
    }).ResponseAsync;

  public async Task Remove()
  {
    await _chillerClient.RemoveChillerAsync(new ChillerRequest { ChillerName = _deviceConfig.DeviceName });
    await OnRemoveCallback();
  }

  public ChillerConfig ChillerConfig { get; }

  public Func<Task> OnRemoveCallback { get; }
  public LaserChillerConfigEditViewModel EditViewModel { get; }
}
