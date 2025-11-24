using Ares.Datamodel.Device;
using Ares.Services.Device;
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

  public Task<DeviceOperationalStatus> GetDeviceOperationalStatus()
  {
    try
    {
      return _devicesClient.GetDeviceStatusAsync(new DeviceStatusRequest { DeviceId = _deviceConfig.UniqueId }).ResponseAsync;
    }
    catch(RpcException)
    {
      return Task.FromResult(new DeviceOperationalStatus { OperationalState = OperationalState.Error, Message = $"Unable to find a registered syringe pump with a name {SyringePumpConfig.Name}" });
    }
  }

  public Task Activate()
    => _devicesClient.ActivateAsync(new DeviceActivateRequest { DeviceId = _deviceConfig.UniqueId }).ResponseAsync;

  public async Task Save()
  {
    var syringePumpConfig = EditViewModel.Save();
    var updateRequest = new SyringePumpUpdateRequest
    {
      Id = _deviceConfig.UniqueId,
      Config = syringePumpConfig
    };

    await _syringePumpClient.UpdateSyringePumpAsync(updateRequest);
  }

  public async Task Remove()
  {
    await _syringePumpClient.RemoveSyringePumpAsync(new SyringePumpRemoveRequest { DeviceId = _deviceConfig.UniqueId });
    await OnRemoveCallback();
  }
}
