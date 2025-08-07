/*namespace UI.Backend.ViewModels.Settings.Device.SerialRestDevice;

public class SerialRestDeviceSettingsViewModel
{

}*/

using Ares.Datamodel.Device;
using Ares.Services.Device;
using Grpc.Core;
using RestSerialDevice.Config;
using RestSerialDevice.Services;
using ReactiveUI;


namespace UI.Backend.ViewModels.Settings.Device.SerialRestDevice;

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

    public async Task<DeviceStatus> GetDeviceStatus()
    {
        try
        {
            return await _devicesClient.GetDeviceStatusAsync(new DeviceStatusRequest { DeviceName = Config.Name });
        }

        catch(RpcException)
        {
            return new DeviceStatus() { DeviceState = DeviceState.Error, Message = $"Unable to find a registered Rest Device with a name {Config.Name}" };
        }
    }

    public async Task Save()
    {
        var servoConfig = EditViewModel.Save();
        await _client.UpdateGenericSerialDeviceAsync(servoConfig);
    }

    public Task Activate()
        => _devicesClient.ActivateAsync(new DeviceActivateRequest
        {
            DeviceName = Config.Name
        }).ResponseAsync;

    public async Task Remove()
    {
        await _client.RemoveGenericSerialDeviceAsync(new DeviceRequest() { DeviceName = _deviceConfig.DeviceName });
        await OnRemoveCallback();
    }

    public RestSerialConfig Config { get; set; }
    public Func<Task> OnRemoveCallback { get; set; }
    public SerialRestDeviceConfigEditViewModel EditViewModel { get; set; }
}

