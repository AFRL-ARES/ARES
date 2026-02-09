using Ares.Datamodel.Device;
using Ares.Services.Device;
using CommunityToolkit.Mvvm.Messaging;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using UI.Features.Devices.ValveController;
using ValveController;
using ValveController.Config;
using ValveController.Services;

namespace UI.Backend.ViewModels.Settings.Device.ValveController;

public partial class ValveControllerSettingsListViewModel : ReactiveObject
{
  private readonly ValveControllerRpc.ValveControllerRpcClient _valveControllerClient;
  private readonly AresDevices.AresDevicesClient _devicesClient;
  private readonly IMessenger _messenger;

  public ValveControllerSettingsListViewModel(AresDevices.AresDevicesClient devicesClient, 
    ValveControllerRpc.ValveControllerRpcClient valveControllerRpcClient, 
    IMessenger messenger)
  {
    _devicesClient = devicesClient;
    _valveControllerClient = valveControllerRpcClient;
    _messenger = messenger;
    UpdateConfigs();
  }

  [Reactive]
  public partial IEnumerable<ValveControllerSettingsViewModel>? SettingsViewModels { get; private set; }

  private void UpdateViewModels(IEnumerable<DeviceConfig> deviceConfigs)
  {
    var viewModels = deviceConfigs.Select(config => new ValveControllerSettingsViewModel(config, _valveControllerClient, _devicesClient, _messenger, OnConfigRemoved));
    SettingsViewModels = viewModels;
  }

  public ValveControllerConfigEditViewModel GetNewConfigEditViewModel() => new(_valveControllerClient, _devicesClient);

  private Task UpdateConfigs()
  {
    SettingsViewModels = null;
    return _devicesClient
      .GetAllDeviceConfigsAsync(new DeviceConfigRequest { DeviceType = typeof(IValveController).FullName })
      .ResponseAsync.ContinueWith(task => UpdateViewModels(task.Result.Configs));
  }

  private async Task OnConfigRemoved()
  {
    SettingsViewModels = null;
    await UpdateConfigs();
  }

  public async Task AddNewConfig(ValveControllerConfig config)
  {
    await _valveControllerClient.AddValveControllerAsync(config);
    await UpdateConfigs();
  }

}
