using Ares.Datamodel.Device;
using Ares.Services.Device;
using Grpc.Core;
using HerkulexDRS.Config;
using HerkulexDRS.Services;
using ReactiveUI;

namespace UI.Backend.ViewModels.Settings.Device.Servo;

public class ServoSettingsViewModel : ReactiveObject
{
  private readonly HerkulexDRSRpc.HerkulexDRSRpcClient _servoClient;
  private readonly DeviceConfig _deviceConfig;
  private readonly AresDevices.AresDevicesClient _devicesClient;

  public ServoSettingsViewModel(DeviceConfig deviceConfig,
    HerkulexDRSRpc.HerkulexDRSRpcClient servoClient,
    AresDevices.AresDevicesClient devicesClient,
    Func<Task> onRemoveCallback)
  {
    _deviceConfig = deviceConfig;
    _servoClient = servoClient;
    ServoConfig = deviceConfig.ConfigData.Unpack<ServoConfig>();
    _devicesClient = devicesClient;
    OnRemoveCallback = onRemoveCallback;
    EditViewModel = new ServoConfigEditViewModel(_servoClient, _devicesClient, ServoConfig);
  }

  public ServoConfig ServoConfig { get; }
  public Func<Task> OnRemoveCallback { get; }
  public ServoConfigEditViewModel EditViewModel { get; }

  public Task<DeviceOperationalStatus> GetDeviceOperationalStatus()
  {
    try
    {
      return _devicesClient.GetDeviceStatusAsync(new DeviceStatusRequest { DeviceId = _deviceConfig.UniqueId }).ResponseAsync;
    }

    catch(RpcException)
    {
      return Task.FromResult(new DeviceOperationalStatus { OperationalState = OperationalState.Error, Message = $"Unable to find a registered Servo with a name {ServoConfig.Name}" });
    }
  }

  public async Task Save()
  {
    var servoConfig = EditViewModel.Save();
    await _servoClient.UpdateServoAsync(servoConfig);
  }

  public Task Activate()
    => _devicesClient.ActivateAsync(new DeviceActivateRequest
    {
      DeviceId = _deviceConfig.UniqueId
    }).ResponseAsync;

  public async Task Remove()
  {
    await _servoClient.RemoveServoAsync(new HerkulexRequest { HerkulexId = _deviceConfig.UniqueId });
    await OnRemoveCallback();
  }

}

