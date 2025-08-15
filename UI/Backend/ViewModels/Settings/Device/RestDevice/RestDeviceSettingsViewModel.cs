using Ares.Datamodel.Device;
using Ares.Services.Device;
using Grpc.Core;
using ReactiveUI;
using RestDevice.Config;
using RestDevice.Services;

namespace UI.Backend.ViewModels.Settings.Device.RestDevice;

public class RestDeviceSettingsViewModel : ReactiveObject
{
  private readonly RestDeviceRpc.RestDeviceRpcClient _client;
  private readonly DeviceConfig _deviceConfig;
  private readonly AresDevices.AresDevicesClient _devicesClient;

  public RestDeviceSettingsViewModel(DeviceConfig deviceConfig,
    RestDeviceRpc.RestDeviceRpcClient restClient,
    AresDevices.AresDevicesClient devicesClient,
    Func<Task> onRemoveCallback)
  {
    _deviceConfig = deviceConfig;
    _client = restClient;
    _devicesClient = devicesClient;
    Config = deviceConfig.ConfigData.Unpack<RestDeviceConfig>();
    OnRemoveCallback = onRemoveCallback;
    EditViewModel = new RestDeviceConfigEditViewModel(_client, _devicesClient, Config);
  }

  public async Task<DeviceOperationalStatus> GetDeviceOperationalStatus()
  {
    try
    {
      return await _devicesClient.GetDeviceStatusAsync(new DeviceStatusRequest { DeviceName = Config.Name });
    }

    catch(RpcException)
    {
      return new DeviceOperationalStatus() { OperationalState = OperationalState.Error, Message = $"Unable to find a registered Rest Device with a name {Config.Name}" };
    }
  }

  public async Task Save()
  {
    var servoConfig = EditViewModel.Save();
    await _client.UpdateRestDeviceAsync(servoConfig);
  }

  public Task Activate()
    => _devicesClient.ActivateAsync(new DeviceActivateRequest
    {
      DeviceName = Config.Name
    }).ResponseAsync;

  public async Task Remove()
  {
    await _client.RemoveRestDeviceAsync(new DeviceRequest() { DeviceName = _deviceConfig.DeviceName });
    await OnRemoveCallback();
  }

  public RestDeviceConfig Config { get; set; }
  public Func<Task> OnRemoveCallback { get; set; }
  public RestDeviceConfigEditViewModel EditViewModel { get; set; }
}
