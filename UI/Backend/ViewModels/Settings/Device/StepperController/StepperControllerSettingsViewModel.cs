using Ares.Datamodel.Device;
using Ares.Services.Device;
using Grpc.Core;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using TicStepperController.Config;
using TicStepperController.Messaging;

namespace UI.Backend.ViewModels.Settings.Device.StepperController;

public class StepperControllerSettingsViewModel : ReactiveObject
{
  private readonly DeviceConfig _deviceConfig;
  private readonly AresDevices.AresDevicesClient _devicesClient;
  private readonly StepperControllerRpc.StepperControllerRpcClient _stepperControllerClient;

  public StepperControllerSettingsViewModel(DeviceConfig deviceConfig,
    StepperControllerRpc.StepperControllerRpcClient stepperControllerClient,
    AresDevices.AresDevicesClient devicesClient,
    Func<Task> onRemoveCallback)
  {
    _deviceConfig = deviceConfig;
    StepperControllerConfig = deviceConfig.ConfigData.Unpack<StepperControllerConfig>();
    _stepperControllerClient = stepperControllerClient;
    _devicesClient = devicesClient;
    OnRemoveCallback = onRemoveCallback;
    EditViewModel = new StepperControllerConfigEditViewModel(_stepperControllerClient, _devicesClient, StepperControllerConfig);
  }

  public StepperControllerConfig StepperControllerConfig { get; }

  public Func<Task> OnRemoveCallback { get; }

  public StepperControllerConfigEditViewModel EditViewModel { get; }

  public async Task<DeviceStatus> GetDeviceStatus()
  {
    try
    {
      var status = await _devicesClient.GetDeviceStatusAsync(new DeviceStatusRequest { DeviceName = StepperControllerConfig.Name }).ResponseAsync;
      DeviceActive = status.DeviceState is DeviceState.Active;
      return status;
    }
    catch(RpcException)
    {
      return new DeviceStatus { DeviceState = DeviceState.Error, Message = $"Unable to find a registered stepper controller with a name {StepperControllerConfig.Name}" };
    }
  }

  public Task Activate()
    => _devicesClient.ActivateAsync(new DeviceActivateRequest { DeviceName = StepperControllerConfig.Name }).ResponseAsync;

  public async Task Save()
  {
    var syringePumpConfig = EditViewModel.Save();
    await _stepperControllerClient.UpdateStepperControllerAsync(syringePumpConfig);
  }

  public async Task Remove()
  {
    await _stepperControllerClient.RemoveStepperControllerAsync(new TicRequest { TicName = _deviceConfig.DeviceName });
    await OnRemoveCallback();
  }

  public async Task Init()
  {
    var status = await GetDeviceStatus();
    if(status.DeviceState != DeviceState.Active)
      return;

    var deviceState = await _stepperControllerClient.GetStateAsync(new TicRequest { TicName = _deviceConfig.DeviceName });

    MaxAcceleration = deviceState.MaxAcceleration;
    MaxDeceleration = deviceState.MaxDeceleration;
    CurrentLimit = deviceState.CurrentLimit;
    StartingSpeed = deviceState.StartingSpeed;
    CustomStepSize = deviceState.CustomStepSize;
    MaxSpeed = deviceState.MaxSpeed;
    StepMode = deviceState.StepMode;
  }

  [Reactive]
  public bool DeviceActive { get; private set; }

  public uint MaxAcceleration { get; private set; }
  public uint MaxDeceleration { get; private set; }
  public uint CurrentLimit { get; private set; }
  public uint StartingSpeed { get; private set; }
  public uint CustomStepSize { get; private set; }
  public uint MaxSpeed { get; private set; }
  public StepMode StepMode { get; private set; }
}
