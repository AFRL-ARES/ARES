using AlicatMFC;
using Ares.Alicat.Mfc.Config;
using Ares.Alicat.Mfc.Messaging;
using Ares.Datamodel.Device;
using Ares.Services.Device;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace UI.Backend.ViewModels.Settings.Device.Mfc;

public class MfcSettingsListViewModel : ReactiveObject
{
  private readonly AresDevices.AresDevicesClient _devicesClient;
  private readonly MfcRpc.MfcRpcClient _mfcClient;

  public MfcSettingsListViewModel(AresDevices.AresDevicesClient devicesClient, MfcRpc.MfcRpcClient mfcClient)
  {
    _devicesClient = devicesClient;
    _mfcClient = mfcClient;
    _ = UpdateConfigs();
  }

  [Reactive]
  public IEnumerable<MfcSettingsViewModel>? SettingsViewModels { get; private set; }

  private void UpdateViewModels(IEnumerable<DeviceConfig> deviceConfigs)
  {
    var viewModels = deviceConfigs.Select(config => new MfcSettingsViewModel(config, _mfcClient, _devicesClient, OnConfigRemoved)).ToArray();
    SettingsViewModels = viewModels;
  }

  public MfcConfigEditViewModel GetNewConfigEditViewModel()
    => new(_mfcClient, _devicesClient);

  private async Task UpdateConfigs()
  {
    SettingsViewModels = null;
    var configs = await _devicesClient
      .GetAllDeviceConfigsAsync(new DeviceConfigRequest { DeviceType = typeof(IMassFlowController).FullName });

    UpdateViewModels(configs.Configs);
  }

  public async Task OnConfigRemoved()
  {
    SettingsViewModels = null;
    await UpdateConfigs();
  }

  public async Task AddNewConfig(MfcConfig config)
  {
    await _mfcClient.AddMfcAsync(config);
    await UpdateConfigs();
  }
}
