using AlicatMFC;
using Ares.Alicat.Mfc.Config;
using Ares.Alicat.Mfc.Messaging;
using Ares.Datamodel.Device;
using Ares.Services.Device;
using CommunityToolkit.Mvvm.Messaging;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace UI.Backend.ViewModels.Settings.Device.Mfc;

public partial class MfcSettingsListViewModel : ReactiveObject
{
  private readonly AresDevices.AresDevicesClient _devicesClient;
  private readonly MfcRpc.MfcRpcClient _mfcClient;
  private readonly IMessenger _messenger;
  private readonly ILoggerFactory _loggerFactory;

  public MfcSettingsListViewModel(AresDevices.AresDevicesClient devicesClient, MfcRpc.MfcRpcClient mfcClient, IMessenger messenger, ILoggerFactory loggerFactory)
  {
    _devicesClient = devicesClient;
    _mfcClient = mfcClient;
    _messenger = messenger;
    _loggerFactory = loggerFactory;
    _ = UpdateConfigs();
  }

  [Reactive]
  public partial IEnumerable<MfcSettingsViewModel>? SettingsViewModels { get; private set; }

  private void UpdateViewModels(IEnumerable<DeviceConfig> deviceConfigs)
  {
    var viewModels = deviceConfigs.Select(config => new MfcSettingsViewModel(config, _mfcClient, _devicesClient, _loggerFactory, _messenger, OnConfigRemoved)).ToArray();
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
