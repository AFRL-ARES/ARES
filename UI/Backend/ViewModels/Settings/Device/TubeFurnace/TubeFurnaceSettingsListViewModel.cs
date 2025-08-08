using Ares.Datamodel.Device;
using Ares.Services.Device;
using ReactiveUI.Fody.Helpers;
using ReactiveUI;
using LindbergFurnace;
using TubeFurnace.Config;
using TubeFurnace.Messaging;

namespace UI.Backend.ViewModels.Settings.Device.TubeFurnace
{
  public class TubeFurnaceSettingsListViewModel : ReactiveObject
  {
    private readonly TubeFurnaceRpc.TubeFurnaceRpcClient _tubeFurnaceClient;
    private readonly AresDevices.AresDevicesClient _devicesClient;

    public TubeFurnaceSettingsListViewModel(AresDevices.AresDevicesClient devicesClient, TubeFurnaceRpc.TubeFurnaceRpcClient tubeFurnaceClient)
    {
      _devicesClient = devicesClient;
      _tubeFurnaceClient = tubeFurnaceClient;
      UpdateConfigs();
    }

    [Reactive]
    public IEnumerable<TubeFurnaceSettingsViewModel>? SettingsViewModels { get; private set; }

    private void UpdateViewModels(IEnumerable<DeviceConfig> deviceConfigs)
    {
      var viewModels = deviceConfigs.Select(config => new TubeFurnaceSettingsViewModel(config, _tubeFurnaceClient, _devicesClient, OnConfigRemoved));
      SettingsViewModels = viewModels;
    }

    public TubeFurnaceConfigEditViewModel GetNewConfigEditViewModel()
      => new(_tubeFurnaceClient, _devicesClient);

    private Task UpdateConfigs()
    {
      SettingsViewModels = null;
      return _devicesClient
        .GetAllDeviceConfigsAsync(new DeviceConfigRequest { DeviceType = typeof(ITubeFurnace).FullName })
        .ResponseAsync.ContinueWith(task => UpdateViewModels(task.Result.Configs));
    }

    public async Task OnConfigRemoved()
    {
      SettingsViewModels = null;
      await UpdateConfigs();
    }

    public async Task AddNewConfig(TubeFurnaceConfig config)
    {
      await _tubeFurnaceClient.AddTubeFurnaceAsync(config);
      await UpdateConfigs();
    }
  }
}
