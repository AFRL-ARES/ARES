
/*
/**
public class SerialRestDeviceSettingsListViewModel
{
}
*#1#

using Ares.Messaging.Device;
using RestSerialDevice;
using RestSerialDevice.Config;
using RestSerialDevice.Services;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace UI.Backend.ViewModels.Settings.Device.SerialRestDevice;

public class SerialRestDeviceSettingsListViewModel : ReactiveObject
{
    private readonly RestSerialDeviceRpc.RestSerialDeviceRpcClient _restSerialClient;
    private readonly AresDevices.AresDevicesClient _devicesClient;
    public SerialRestDeviceSettingsListViewModel(AresDevices.AresDevicesClient devicesClient, RestSerialDeviceRpc.RestSerialDeviceRpcClient restSerialClient)
    {
        _restSerialClient = restSerialClient;
        _devicesClient = devicesClient;
        UpdateConfigs();
    }

    [Reactive]
    public IEnumerable<SerialRestDeviceSettingsViewModel>? SettingsViewModels { get; private set; }

    private void UpdateViewModels(IEnumerable<DeviceConfig> deviceConfigs)
    {
        var viewModels = deviceConfigs.Select(config => new SerialRestDeviceSettingsViewModel(config, _restSerialClient, _devicesClient, OnConfigRemoved));
        SettingsViewModels = viewModels;
    }

    public SerialRestDeviceConfigEditViewModel GetNewConfigEditViewModel()
        => new(_restSerialClient, _devicesClient);

    private Task UpdateConfigs()
    {
        SettingsViewModels = null;
        return _devicesClient
            .GetAllDeviceConfigsAsync(new DeviceConfigRequest { DeviceType = typeof(ISerialRestDevice).FullName })
            .ResponseAsync.ContinueWith(task => UpdateViewModels(task.Result.Configs));
    }

    private async Task OnConfigRemoved()
    {
        SettingsViewModels = null;
        await UpdateConfigs();
    }

    public async Task AddNewConfig(RestSerialConfig config)
    {
        await _restSerialClient.AddGenericSerialDeviceAsync(config);
        await UpdateConfigs();
    }
}
*/


using Ares.Datamodel.Device;
using Ares.Services.Device;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using RestSerialDevice;
using RestSerialDevice.Config;
using RestSerialDevice.Services;

namespace UI.Backend.ViewModels.Settings.Device.SerialRestDevice;

public class SerialRestDeviceSettingsListViewModel : ReactiveObject
{
    private readonly RestSerialDeviceRpc.RestSerialDeviceRpcClient _restClient;
    private readonly AresDevices.AresDevicesClient _devicesClient;

    public SerialRestDeviceSettingsListViewModel(AresDevices.AresDevicesClient devicesClient, RestSerialDeviceRpc.RestSerialDeviceRpcClient restClient)
    {
        _restClient = restClient;
        _devicesClient = devicesClient;
        UpdateConfigs();
    }

    [Reactive]
    public IEnumerable<SerialRestDeviceSettingsViewModel>? SettingsViewModels { get; private set; }

    private void UpdateViewModels(IEnumerable<DeviceConfig> deviceConfigs)
    {
        var viewModels = deviceConfigs.Select(config => new SerialRestDeviceSettingsViewModel(config, _restClient, _devicesClient, OnConfigRemoved));
        SettingsViewModels = viewModels;
    }

    public SerialRestDeviceConfigEditViewModel GetNewConfigEditViewModel()
        => new(_restClient, _devicesClient);

    private Task UpdateConfigs()
    {
        SettingsViewModels = null;
        return _devicesClient
            .GetAllDeviceConfigsAsync(new DeviceConfigRequest { DeviceType = typeof(ISerialRestDevice).FullName })
            .ResponseAsync.ContinueWith(task => UpdateViewModels(task.Result.Configs));
    }

    private async Task OnConfigRemoved()
    {
        SettingsViewModels = null;
        await UpdateConfigs();
    }

    public async Task AddNewConfig(RestSerialConfig config)
    {
        await _restClient.AddGenericSerialDeviceAsync(config);
        await UpdateConfigs();
    }
}


