using Ares.Datamodel.Device;
using Ares.Services.Device;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using VerdiV6.Config;
using VerdiV6.Services;
using VerdiV6Laser;

namespace UI.Backend.ViewModels.Settings.Device.VerdiLaser
{
  public class VerdiLaserSettingsListViewModel : ReactiveObject
  {
    private readonly VerdiV6Rpc.VerdiV6RpcClient _laserClient;
    private readonly AresDevices.AresDevicesClient _devicesClient;
    public VerdiLaserSettingsListViewModel(AresDevices.AresDevicesClient devicesClient, VerdiV6Rpc.VerdiV6RpcClient laserClient)
    {
      _laserClient = laserClient;
      _devicesClient = devicesClient;
      UpdateConfigs();
    }

    [Reactive]
    public IEnumerable<VerdiLaserSettingsViewModel>? SettingsViewModels { get; private set; }

    private void UpdateViewModels(IEnumerable<DeviceConfig> deviceConfigs)
    {
      var viewModels = deviceConfigs.Select(config => new VerdiLaserSettingsViewModel(config, _laserClient, _devicesClient, OnConfigRemoved));
      SettingsViewModels = viewModels;
    }

    public VerdiLaserConfigEditViewModel GetNewConfigEditViewModel()
  => new(_laserClient, _devicesClient);

    private Task UpdateConfigs()
    {
      SettingsViewModels = null;
      return _devicesClient
        .GetAllDeviceConfigsAsync(new DeviceConfigRequest { DeviceType = typeof(IVerdiV6Laser).FullName })
        .ResponseAsync.ContinueWith(task => UpdateViewModels(task.Result.Configs));
    }

    private async Task OnConfigRemoved()
    {
      SettingsViewModels = null;
      await UpdateConfigs();
    }

    public async Task AddNewConfig(VerdiConfig config)
    {
      await _laserClient.AddLaserAsync(config);
      await UpdateConfigs();
    }
  }
}
