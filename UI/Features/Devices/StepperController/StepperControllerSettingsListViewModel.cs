using Ares.Datamodel.Device;
using Ares.Services.Device;
using CommunityToolkit.Mvvm.Messaging;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using TicStepperController;
using TicStepperController.Config;
using TicStepperController.Messaging;

namespace UI.Backend.ViewModels.Settings.Device.StepperController;

public partial class StepperControllerSettingsListViewModel : ReactiveObject
{
  private readonly StepperControllerRpc.StepperControllerRpcClient _stepperControllerClient;
  private readonly AresDevices.AresDevicesClient _devicesClient;
  private readonly IMessenger _messenger;

  public StepperControllerSettingsListViewModel(AresDevices.AresDevicesClient devicesClient, StepperControllerRpc.StepperControllerRpcClient stepperControllerClient, IMessenger messenger)
  {
    _devicesClient = devicesClient;
    _stepperControllerClient = stepperControllerClient;
    _messenger = messenger;
    UpdateConfigs();
  }

  [Reactive]
  public partial IEnumerable<StepperControllerSettingsViewModel>? SettingsViewModels { get; private set; }

  private void UpdateViewModels(IEnumerable<DeviceConfig> deviceConfigs)
  {
    var viewModels = deviceConfigs.Select(config => new StepperControllerSettingsViewModel(config, _stepperControllerClient, _devicesClient, _messenger, OnConfigRemoved));
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
