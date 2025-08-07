using Ares.Datamodel.Device;
using Ares.Services.Device;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using ValveController;
using ValveController.Config;
using ValveController.Services;

namespace UI.Backend.ViewModels.Settings.Device.ValveController;

public class ValveControllerSettingsListViewModel : ReactiveObject
{
  private readonly ValveControllerRpc.ValveControllerRpcClient _valveControllerClient;
  private readonly AresDevices.AresDevicesClient _devicesClient;

  public ValveControllerSettingsListViewModel(AresDevices.AresDevicesClient devicesClient, ValveControllerRpc.ValveControllerRpcClient valveControllerRpcClient)
  {
    _devicesClient = devicesClient;
    _valveControllerClient = valveControllerRpcClient;
    UpdateConfigs();
  }

  [Reactive]
  public IEnumerable<ValveControllerSettingsViewModel>? SettingsViewModels { get; private set; }

  private void UpdateViewModels(IEnumerable<DeviceConfig> deviceConfigs)
  {
    var viewModels = deviceConfigs.Select(config => new ValveControllerSettingsViewModel(config, _valveControllerClient, _devicesClient, OnConfigRemoved));
    SettingsViewModels = viewModels;
  }

  public ValveControllerConfigEditViewModel GetNewConfigEditViewModel() => new(_valveControllerClient, _devicesClient);

  private Task UpdateConfigs()
  {
    SettingsViewModels = null;
    return _devicesClient
      .GetAllDeviceConfigsAsync(new DeviceConfigRequest { DeviceType = typeof(IValveController).FullName })
      .ResponseAsync.ContinueWith(task => UpdateViewModels(task.Result.Configs));
  }

  private async Task OnConfigRemoved()
  {
    SettingsViewModels = null;
    await UpdateConfigs();
  }

  public async Task AddNewConfig(ValveControllerConfig config)
  {
    await _valveControllerClient.AddValveControllerAsync(config);
    await UpdateConfigs();
  }

}
