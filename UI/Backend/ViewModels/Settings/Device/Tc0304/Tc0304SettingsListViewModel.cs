using Ares.Messaging.Device;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using TC0304;
using Tc0304.Config;
using Tc0304.Services;

namespace UI.Backend.ViewModels.Settings.Device.Tc0304;

public class Tc0304SettingsListViewModel : ReactiveObject
{
  private readonly TC0304Rpc.TC0304RpcClient _dataloggerClient;
  private readonly AresDevices.AresDevicesClient _devicesClient;

  public Tc0304SettingsListViewModel(AresDevices.AresDevicesClient devicesClient, TC0304Rpc.TC0304RpcClient dataloggerClient)
  {
    _devicesClient = devicesClient;
    _dataloggerClient = dataloggerClient;
    UpdateConfigs();
  }

  [Reactive]
  public IEnumerable<Tc0304SettingsViewModel>? SettingsViewModels { get; private set; }

  private void UpdateViewModels(IEnumerable<DeviceConfig> deviceConfigs)
  {
    var viewModels = deviceConfigs.Select(config => new Tc0304SettingsViewModel(config, _dataloggerClient, _devicesClient, OnConfigRemoved));
    SettingsViewModels = viewModels;
  }

  public Tc0304ConfigEditViewModel GetNewConfigEditViewModel()
    => new(_dataloggerClient, _devicesClient);

  private Task UpdateConfigs()
  {
    SettingsViewModels = null;
    return _devicesClient
      .GetAllDeviceConfigsAsync(new DeviceConfigRequest { DeviceType = typeof(IDataloggerThermometer).FullName })
      .ResponseAsync.ContinueWith(task => UpdateViewModels(task.Result.Configs));
  }

  public async Task OnConfigRemoved()
  {
    SettingsViewModels = null;
    await UpdateConfigs();
  }

  public async Task AddNewConfig(Tc0304Config config)
  {
    await _dataloggerClient.AddTc0304Async(config);
    await UpdateConfigs();
  }
}
