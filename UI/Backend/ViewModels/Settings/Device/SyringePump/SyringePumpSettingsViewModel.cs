using Ares.Messaging.Device;
using Ares.SyringePump.Ne1000.Messaging;
using Grpc.Core;
using ReactiveUI;

namespace UI.Backend.ViewModels.Settings.Device.SyringePump;

public class SyringePumpSettingsViewModel : ReactiveObject
{
  private readonly DeviceConfig _deviceConfig;
  private readonly AresDevices.AresDevicesClient _devicesClient;
  private readonly SyringePumpRpc.SyringePumpRpcClient _syringePumpClient;

  public SyringePumpSettingsViewModel(DeviceConfig deviceConfig,
    SyringePumpRpc.SyringePumpRpcClient syringePumpClient,
    AresDevices.AresDevicesClient devicesClient,
    Func<Task> onRemoveCallback)
  {
    _deviceConfig = deviceConfig;
    SyringePumpConfig = deviceConfig.ConfigData.Unpack<SyringePumpConfig>();
    _syringePumpClient = syringePumpClient;
    _devicesClient = devicesClient;
    OnRemoveCallback = onRemoveCallback;
    EditViewModel = new SyringePumpConfigEditViewModel(_syringePumpClient, _devicesClient, SyringePumpConfig);
  }

  public SyringePumpConfig SyringePumpConfig { get; }

  public Func<Task> OnRemoveCallback { get; }

  public SyringePumpConfigEditViewModel EditViewModel { get; }

  public Task<DeviceStatus> GetDeviceStatus()
  {
    try
    {
      return _devicesClient.GetDeviceStatusAsync(new DeviceStatusRequest { DeviceName = SyringePumpConfig.Name }).ResponseAsync;
    }
    catch (RpcException)
    {
      return Task.FromResult(new DeviceStatus { DeviceState = DeviceState.Error, Message = $"Unable to find a registered syringe pump with a name {SyringePumpConfig.Name}" });
    }
  }

  public Task Activate()
    => _devicesClient.ActivateAsync(new DeviceActivateRequest { DeviceName = SyringePumpConfig.Name }).ResponseAsync;

  public async Task Save()
  {
    var syringePumpConfig = EditViewModel.Save();
    await _syringePumpClient.UpdateSyringePumpAsync(syringePumpConfig);
  }

  public async Task Remove()
  {
    await _syringePumpClient.RemoveSyringePumpAsync(new SyringePumpRemoveRequest { DeviceId = _deviceConfig.DeviceName });
    await OnRemoveCallback();
  }
}
