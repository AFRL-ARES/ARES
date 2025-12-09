using Ares.Datamodel.Device;
using Ares.Services.Device;
using ChemyxPumpPlugin.Config;
using ChemyxPumpPlugin.Services;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace UI.Backend.ViewModels.Settings.Device.ChemyxPump;

public class ChemyxPumpSettingsListViewModel : ReactiveObject
{
  private readonly ChemyxPumpRpc.ChemyxPumpRpcClient _client;
  private readonly AresDevices.AresDevicesClient _devicesClient;

  public ChemyxPumpSettingsListViewModel(AresDevices.AresDevicesClient devicesClient, ChemyxPumpRpc.ChemyxPumpRpcClient pumpClient)
  {
    _client = pumpClient;
    _devicesClient = devicesClient;
    UpdateConfigs();
  }

  [Reactive]
  public IEnumerable<ChemyxPumpSettingsViewModel>? SettingsViewModels { get; private set; }
  
  private void UpdateViewModels(IEnumerable<DeviceConfig> deviceConfigs)
  {
    var viewModels = deviceConfigs.Select(config => new ChemyxPumpSettingsViewModel(config, _client, _devicesClient, OnConfigRemoved));
    SettingsViewModels = viewModels;
  }

  public ChemyxPumpConfigEditViewModel GetNewConfigEditViewModel()
    => new(_client, _devicesClient);

  private Task UpdateConfigs()
  {
    SettingsViewModels = null;
    return _devicesClient
      .GetAllDeviceConfigsAsync(new DeviceConfigRequest { DeviceType = typeof(ChemyxPumpPlugin.IChemyxPump).FullName})
      .ResponseAsync.ContinueWith(task => UpdateViewModels(task.Result.Configs));
  }

  private async Task OnConfigRemoved()
  {
    SettingsViewModels = null;
    await UpdateConfigs();
  }

  public async Task AddNewConfig(ChemyxPumpConfig config)
  {
    await _client.AddChemyxPumpAsync(config);
    await UpdateConfigs();
  }
}
