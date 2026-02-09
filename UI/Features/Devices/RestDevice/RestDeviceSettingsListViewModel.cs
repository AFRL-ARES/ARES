using Ares.Datamodel.Device;
using Ares.Services.Device;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using RestDevice;
using RestDevice.Config;
using RestDevice.Services;

namespace UI.Features.Devices.RestDevice;

public partial class RestDeviceSettingsListViewModel : ReactiveObject
{
  private readonly RestDeviceRpc.RestDeviceRpcClient _restClient;
  private readonly AresDevices.AresDevicesClient _devicesClient;

  public RestDeviceSettingsListViewModel(AresDevices.AresDevicesClient devicesClient, RestDeviceRpc.RestDeviceRpcClient restClient)
  {
    _restClient = restClient;
    _devicesClient = devicesClient;
    UpdateConfigs();
  }

  [Reactive]
  public partial IEnumerable<RestDeviceSettingsViewModel>? SettingsViewModels { get; private set; }

  private void UpdateViewModels(IEnumerable<DeviceConfig> deviceConfigs)
  {
    var viewModels = deviceConfigs.Select(config => new RestDeviceSettingsViewModel(config, _restClient, _devicesClient, OnConfigRemoved));
    SettingsViewModels = viewModels;
  }

  public RestDeviceConfigEditViewModel GetNewConfigEditViewModel()
    => new(_restClient, _devicesClient);

  private Task UpdateConfigs()
  {
    SettingsViewModels = null;
    return _devicesClient
      .GetAllDeviceConfigsAsync(new DeviceConfigRequest { DeviceType = typeof(IRestDevice).FullName })
      .ResponseAsync.ContinueWith(task => UpdateViewModels(task.Result.Configs));
  }

  private async Task OnConfigRemoved()
  {
    SettingsViewModels = null;
    await UpdateConfigs();
  }

  public async Task AddNewConfig(RestDeviceConfig config)
  {
    await _restClient.AddRestDeviceAsync(config);
    await UpdateConfigs();
  }
}
