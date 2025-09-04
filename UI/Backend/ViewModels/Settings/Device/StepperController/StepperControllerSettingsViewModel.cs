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

  public async Task<DeviceOperationalStatus> GetDeviceOperationalStatus()
  {
    try
    {
      var status = await _devicesClient.GetDeviceStatusAsync(new DeviceStatusRequest { DeviceId = _deviceConfig.UniqueId }).ResponseAsync;
      DeviceActive = status.OperationalState is OperationalState.Active;
      return status;
    }
    catch(RpcException)
    {
      return new DeviceOperationalStatus { OperationalState = OperationalState.Error, Message = $"Unable to find a registered stepper controller with a name {StepperControllerConfig.Name}" };
    }
  }

  public Task Activate()
    => _devicesClient.ActivateAsync(new DeviceActivateRequest { DeviceId = _deviceConfig.UniqueId }).ResponseAsync;

  public async Task Save()
  {
    var stepperControllerConfig = EditViewModel.Save();
    var updateRequest = new StepperControllerUpdateRequest
    {
      Id = _deviceConfig.UniqueId,
      Config = stepperControllerConfig
    };

    await _stepperControllerClient.UpdateStepperControllerAsync(updateRequest);
  }

  public async Task Remove()
  {
    await _stepperControllerClient.RemoveStepperControllerAsync(new TicRequest { TicId = _deviceConfig.UniqueId });
    await OnRemoveCallback();
  }

  public async Task Init()
  {
    var status = await GetDeviceOperationalStatus();
    if(status.OperationalState != OperationalState.Active)
      return;

    var state = await _stepperControllerClient.GetStateAsync(new TicRequest { TicId = _deviceConfig.UniqueId });

    MaxAcceleration = state.MaxAcceleration;
    MaxDeceleration = state.MaxDeceleration;
    CurrentLimit = state.CurrentLimit;
    StartingSpeed = state.StartingSpeed;
    CustomStepSize = state.CustomStepSize;
    MaxSpeed = state.MaxSpeed;
    StepMode = state.StepMode;
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
