using Ares.Datamodel.Device;
using Ares.Services.Device;
using CommunityToolkit.Mvvm.Messaging;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using VerdiV6.Config;
using VerdiV6.Services;
using VerdiV6Laser;

namespace UI.Features.Devices.VerdiV6Laser
{
  public partial class VerdiLaserSettingsListViewModel : ReactiveObject
  {
    private readonly VerdiV6Rpc.VerdiV6RpcClient _laserClient;
    private readonly AresDevices.AresDevicesClient _devicesClient;
    private IMessenger _messenger;

    public VerdiLaserSettingsListViewModel(AresDevices.AresDevicesClient devicesClient, VerdiV6Rpc.VerdiV6RpcClient laserClient, IMessenger messenger)
    {
      _laserClient = laserClient;
      _devicesClient = devicesClient;
      _messenger = messenger;
      UpdateConfigs();
    }

    [Reactive]
    public partial IEnumerable<VerdiLaserSettingsViewModel>? SettingsViewModels { get; private set; }

    private void UpdateViewModels(IEnumerable<DeviceConfig> deviceConfigs)
    {
      var viewModels = deviceConfigs.Select(config => new VerdiLaserSettingsViewModel(config, _laserClient, _devicesClient, _messenger, OnConfigRemoved));
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
