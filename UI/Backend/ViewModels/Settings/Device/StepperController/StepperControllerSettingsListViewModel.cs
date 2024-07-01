using Ares.Messaging.Device;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using TicStepperController;
using TicStepperController.Config;
using TicStepperController.Messaging;

namespace UI.Backend.ViewModels.Settings.Device.StepperController;

public class StepperControllerSettingsListViewModel : ReactiveObject
{
  private readonly StepperControllerRpc.StepperControllerRpcClient _stepperControllerClient;
  private readonly AresDevices.AresDevicesClient _devicesClient;

  public StepperControllerSettingsListViewModel(AresDevices.AresDevicesClient devicesClient, StepperControllerRpc.StepperControllerRpcClient stepperControllerClient)
  {
    _devicesClient = devicesClient;
    _stepperControllerClient = stepperControllerClient;
    UpdateConfigs();
  }

  [Reactive]
  public IEnumerable<StepperControllerSettingsViewModel>? SettingsViewModels { get; private set; }

  private void UpdateViewModels(IEnumerable<DeviceConfig> deviceConfigs)
  {
    var viewModels = deviceConfigs.Select(config => new StepperControllerSettingsViewModel(config, _stepperControllerClient, _devicesClient, OnConfigRemoved));
    SettingsViewModels = viewModels;
  }

  public StepperControllerConfigEditViewModel GetNewConfigEditViewModel()
    => new(_stepperControllerClient, _devicesClient);

  private Task UpdateConfigs()
  {
    SettingsViewModels = null;
    return _devicesClient
      .GetAllDeviceConfigsAsync(new DeviceConfigRequest { DeviceType = typeof(IStepperController).FullName })
      .ResponseAsync.ContinueWith(task => UpdateViewModels(task.Result.Configs));
  }

  public async Task OnConfigRemoved()
  {
    SettingsViewModels = null;
    await UpdateConfigs();
  }

  public async Task AddNewConfig(StepperControllerConfig config)
  {
    await _stepperControllerClient.AddStepperControllerAsync(config);
    await UpdateConfigs();
  }
}
