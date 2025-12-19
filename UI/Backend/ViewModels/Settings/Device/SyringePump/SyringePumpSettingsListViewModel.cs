using Ares.Datamodel.Device;
using Ares.Services.Device;
using Ares.SyringePump.Ne1000.Messaging;
using CommunityToolkit.Mvvm.Messaging;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SyringePumpNE1000;

namespace UI.Backend.ViewModels.Settings.Device.SyringePump;

public class SyringePumpSettingsListViewModel : ReactiveObject
{
  private readonly AresDevices.AresDevicesClient _devicesClient;
  private readonly SyringePumpRpc.SyringePumpRpcClient _syringePumpClient;
  private readonly IMessenger _messenger;

  public SyringePumpSettingsListViewModel(AresDevices.AresDevicesClient devicesClient, SyringePumpRpc.SyringePumpRpcClient syringePumpClient, IMessenger messenger)
  {
    _devicesClient = devicesClient;
    _syringePumpClient = syringePumpClient;
    _messenger = messenger;
    _ = UpdateConfigs();
  }

  [Reactive]
  public IEnumerable<SyringePumpSettingsViewModel>? SettingsViewModels { get; private set; }

  private void UpdateViewModels(IEnumerable<DeviceConfig> deviceConfigs)
  {
    var viewModels = deviceConfigs.Select(config => new SyringePumpSettingsViewModel(config, _syringePumpClient, _devicesClient, _messenger, OnConfigRemoved));
    SettingsViewModels = viewModels;
  }

  public SyringePumpConfigEditViewModel GetNewConfigEditViewModel()
    => new(_syringePumpClient, _devicesClient);

  private async Task UpdateConfigs()
  {
    SettingsViewModels = null;
    var configs = await _devicesClient
      .GetAllDeviceConfigsAsync(new DeviceConfigRequest { DeviceType = typeof(ISyringePump).FullName });

    UpdateViewModels(configs.Configs);
  }

  public async Task OnConfigRemoved()
  {
    SettingsViewModels = null;
    await UpdateConfigs();
  }

  public async Task AddNewConfig(SyringePumpConfig config)
  {
    await _syringePumpClient.AddSyringePumpAsync(config);
    await UpdateConfigs();
  }
}
