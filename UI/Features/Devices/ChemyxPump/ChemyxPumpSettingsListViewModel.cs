using Ares.Datamodel.Device;
using Ares.Services.Device;
using ChemyxPumpPlugin.Config;
using ChemyxPumpPlugin.Services;
using CommunityToolkit.Mvvm.Messaging;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using UI.Features.Devices.ChemyxPump;

namespace UI.Backend.ViewModels.Settings.Device.ChemyxPump;

public partial class ChemyxPumpSettingsListViewModel : ReactiveObject
{
  private readonly ChemyxPumpRpc.ChemyxPumpRpcClient _client;
  private readonly AresDevices.AresDevicesClient _devicesClient;
  private readonly IMessenger _deviceDeletionMessenger;

  public ChemyxPumpSettingsListViewModel(AresDevices.AresDevicesClient devicesClient, ChemyxPumpRpc.ChemyxPumpRpcClient pumpClient, IMessenger deviceDeletionMessenger)
  {
    _client = pumpClient;
    _devicesClient = devicesClient;
    _deviceDeletionMessenger = deviceDeletionMessenger;
    UpdateConfigs();
  }

  [Reactive]
  public partial IEnumerable<ChemyxPumpSettingsViewModel>? SettingsViewModels { get; private set; }
  
  private void UpdateViewModels(IEnumerable<DeviceConfig> deviceConfigs)
  {
    var viewModels = deviceConfigs.Select(config => new ChemyxPumpSettingsViewModel(config, _client, _devicesClient, _deviceDeletionMessenger, OnConfigRemoved));
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
