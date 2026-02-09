/*namespace UI.Backend.ViewModels.Settings.Device.SerialRestDevice;

public class SerialRestDeviceSettingsViewModel
{

}*/

using Ares.Datamodel.Device;
using Ares.Services.Device;
using Grpc.Core;
using ReactiveUI;
using RestSerialDevice.Config;
using RestSerialDevice.Services;
using UI.Backend.ViewModels.Settings.Device.SerialRestDevice;

namespace UI.Features.Devices.SerialRestDevice;

public class SerialRestDeviceSettingsViewModel : ReactiveObject
{
  private readonly RestSerialDeviceRpc.RestSerialDeviceRpcClient _client;
  private readonly DeviceConfig _deviceConfig;
  private readonly AresDevices.AresDevicesClient _devicesClient;

  public SerialRestDeviceSettingsViewModel(DeviceConfig deviceConfig,
      RestSerialDeviceRpc.RestSerialDeviceRpcClient restClient,
      AresDevices.AresDevicesClient devicesClient,
      Func<Task> onRemoveCallback)
  {
    _deviceConfig = deviceConfig;
    _client = restClient;
    _devicesClient = devicesClient;
    Config = deviceConfig.ConfigData.Unpack<RestSerialConfig>();
    OnRemoveCallback = onRemoveCallback;
    EditViewModel = new SerialRestDeviceConfigEditViewModel(_client, _devicesClient, Config);
  }

  public async Task<DeviceOperationalStatus> GetDeviceOperationalStatus()
  {
    try
    {
      return await _devicesClient.GetDeviceStatusAsync(new DeviceStatusRequest { DeviceId = _deviceConfig.UniqueId });
    }

    catch(RpcException)
    {
      return new DeviceOperationalStatus() { OperationalState = OperationalState.Error, Message = $"Unable to find a registered Rest Device with a name {Config.Name}" };
    }
  }

  public async Task Save()
  {
    var config = EditViewModel.Save();
    var updateRequest = new GenericSerialRestDeviceUpdateRequest
    {
      Id = _deviceConfig.UniqueId,
      Config = config,
    };

    await _client.UpdateGenericSerialDeviceAsync(updateRequest);
  }

  public Task Activate()
      => _devicesClient.ActivateAsync(new DeviceActivateRequest
      {
        DeviceId = _deviceConfig.UniqueId
      }).ResponseAsync;

  public async Task Remove()
  {
    await _client.RemoveGenericSerialDeviceAsync(new DeviceRequest() { DeviceId = _deviceConfig.UniqueId });
    await OnRemoveCallback();
  }

  public RestSerialConfig Config { get; set; }
  public Func<Task> OnRemoveCallback { get; set; }
  public SerialRestDeviceConfigEditViewModel EditViewModel { get; set; }
}

