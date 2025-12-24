using Ares.Datamodel.Device;
using Ares.Services.Device;
using Chiller.Config;
using Chiller.Services;
using CommunityToolkit.Mvvm.Messaging;
using LaserChiller;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace UI.Backend.ViewModels.Settings.Device.LaserChiller;

public partial class LaserChillerSettingsListViewModel : ReactiveObject
{
  private readonly ChillerRpc.ChillerRpcClient _chillerClient;
  private readonly AresDevices.AresDevicesClient _devicesClient;
  private readonly IMessenger _messenger;

  public LaserChillerSettingsListViewModel(AresDevices.AresDevicesClient devicesClient, ChillerRpc.ChillerRpcClient chillerClient, IMessenger messenger)
  {
    _chillerClient = chillerClient;
    _devicesClient = devicesClient;
    _messenger = messenger;
    UpdateConfigs();
  }

  [Reactive]
  public partial IEnumerable<LaserChillerSettingsViewModel>? SettingsViewModels { get; private set; }

  private void UpdateViewModels(IEnumerable<DeviceConfig> deviceConfigs)
  {
    var viewModels = deviceConfigs.Select(config => new LaserChillerSettingsViewModel(config, _chillerClient, _devicesClient, _messenger, OnConfigRemoved));
    SettingsViewModels = viewModels;
  }

  public LaserChillerConfigEditViewModel GetNewConfigEditViewModel()
=> new(_chillerClient, _devicesClient);

  private Task UpdateConfigs()
  {
    SettingsViewModels = null;
    return _devicesClient
      .GetAllDeviceConfigsAsync(new DeviceConfigRequest { DeviceType = typeof(ILaserChiller).FullName })
      .ResponseAsync.ContinueWith(task => UpdateViewModels(task.Result.Configs));
  }

  private async Task OnConfigRemoved()
  {
    SettingsViewModels = null;
    await UpdateConfigs();
  }

  public async Task AddNewConfig(ChillerConfig config)
  {
    await _chillerClient.AddChillerAsync(config);
    await UpdateConfigs();
  }
}
