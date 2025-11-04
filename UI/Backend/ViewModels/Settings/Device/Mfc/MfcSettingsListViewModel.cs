using AlicatMFC;
using Ares.Alicat.Mfc.Config;
using Ares.Alicat.Mfc.Messaging;
using Ares.Datamodel.Device;
using Ares.Services.Device;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace UI.Backend.ViewModels.Settings.Device.Mfc;

public class MfcSettingsListViewModel : ReactiveObject
{
  private readonly AresDevices.AresDevicesClient _devicesClient;
  private readonly MfcRpc.MfcRpcClient _mfcClient;
  private readonly ILoggerFactory _loggerFactory;

  public MfcSettingsListViewModel(AresDevices.AresDevicesClient devicesClient, MfcRpc.MfcRpcClient mfcClient, ILoggerFactory loggerFactory)
  {
    _devicesClient = devicesClient;
    _mfcClient = mfcClient;
    _loggerFactory = loggerFactory;
    _ = UpdateConfigs();
  }

  [Reactive]
  public IEnumerable<MfcSettingsViewModel>? SettingsViewModels { get; private set; }

  private void UpdateViewModels(IEnumerable<DeviceConfig> deviceConfigs)
  {
    var viewModels = deviceConfigs.Select(config => new MfcSettingsViewModel(config, _mfcClient, _devicesClient, _loggerFactory, OnConfigRemoved)).ToArray();
    SettingsViewModels = viewModels;
  }

  public MfcConfigEditViewModel GetNewConfigEditViewModel()
    => new(_mfcClient, _devicesClient, _loggerFactory.CreateLogger<MfcConfigEditViewModel>());

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
