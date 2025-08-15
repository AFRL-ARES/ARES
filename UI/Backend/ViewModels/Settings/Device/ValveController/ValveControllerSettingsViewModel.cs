using Ares.Datamodel.Device;
using Ares.Services.Device;
using Grpc.Core;
using ReactiveUI;
using ValveController.Config;
using ValveController.Services;

namespace UI.Backend.ViewModels.Settings.Device.ValveController;

public class ValveControllerSettingsViewModel : ReactiveObject
{
  private readonly ValveControllerRpc.ValveControllerRpcClient _valveControllerClient;
  private readonly DeviceConfig _deviceConfig;
  private readonly AresDevices.AresDevicesClient _devicesClient;

  public ValveControllerSettingsViewModel(DeviceConfig deviceConfig,
    ValveControllerRpc.ValveControllerRpcClient valveControllerClient,
    AresDevices.AresDevicesClient devicesClient,
    Func<Task> onRemoveCallback)
  {
    _valveControllerClient = valveControllerClient;
    _deviceConfig = deviceConfig;
    ValveControllerConfig = deviceConfig.ConfigData.Unpack<ValveControllerConfig>();
    _devicesClient = devicesClient;
    OnRemoveCallback = onRemoveCallback;
    EditViewModel = new ValveControllerConfigEditViewModel(_valveControllerClient, _devicesClient, ValveControllerConfig);
  }

  public ValveControllerConfig ValveControllerConfig { get; }

  public Func<Task> OnRemoveCallback { get; }

  public ValveControllerConfigEditViewModel EditViewModel { get; }

  public Task<DeviceOperationalStatus> GetDeviceOperationalStatus()
  {
    try
    {
      return _devicesClient.GetDeviceStatusAsync(new DeviceStatusRequest { DeviceName = ValveControllerConfig.Name }).ResponseAsync;
    }

    catch (RpcException)
    {
      return Task.FromResult(new DeviceOperationalStatus { OperationalState = OperationalState.Error, Message = $"Unable to find a registered Valve Controller with a name {ValveControllerConfig.Name}" });
    }
  }

  public async Task Save()
  {
    var valveControllerConfig = EditViewModel.Save();
    await _valveControllerClient.UpdateValveControllersAsync(valveControllerConfig);
  }

  public Task Activate()
    => _devicesClient.ActivateAsync(new DeviceActivateRequest
    {
      DeviceName = ValveControllerConfig.Name
    }).ResponseAsync;

  public async Task Remove()
  {
    await _valveControllerClient.RemoveValveControllerAsync(new ValveControllerRequest { DeviceName = _deviceConfig.DeviceName });
    await OnRemoveCallback();
  }
}
