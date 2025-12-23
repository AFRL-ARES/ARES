using Ares.Datamodel.Device;
using Ares.Services.Device;
using Chiller.Config;
using Chiller.Services;
using CommunityToolkit.Mvvm.Messaging;
using Grpc.Core;
using ReactiveUI;
using UI.Backend.Devices;

namespace UI.Backend.ViewModels.Settings.Device.LaserChiller;

public class LaserChillerSettingsViewModel : ReactiveObject
{
  private readonly ChillerRpc.ChillerRpcClient _chillerClient;
  private readonly DeviceConfig _deviceConfig;
  private readonly AresDevices.AresDevicesClient _devicesClient;
  private readonly IMessenger _messenger;

  public LaserChillerSettingsViewModel(DeviceConfig deviceConfig,
    ChillerRpc.ChillerRpcClient chillerClient,
    AresDevices.AresDevicesClient devicesClient,
    IMessenger messenger,
    Func<Task> onRemoveCallback)
  {
    _deviceConfig = deviceConfig;
    _chillerClient = chillerClient;
    _devicesClient = devicesClient;
    _messenger = messenger;
    OnRemoveCallback = onRemoveCallback;
    ChillerConfig = deviceConfig.ConfigData.Unpack<ChillerConfig>();
    EditViewModel = new LaserChillerConfigEditViewModel(_chillerClient, _devicesClient, ChillerConfig);
  }

  public Task<DeviceOperationalStatus> GetDeviceOperationalStatus()
  {
    try
    {
      return _devicesClient.GetDeviceStatusAsync(new DeviceStatusRequest { DeviceId = _deviceConfig.UniqueId }).ResponseAsync;
    }

    catch(RpcException)
    {
      return Task.FromResult(new DeviceOperationalStatus { OperationalState = OperationalState.Error, Message = $"Unable to find a registered V6 Laser with a name {ChillerConfig.Name}" });
    }
  }

  public async Task Save()
  {
    var laserConfig = EditViewModel.Save();
    var updateRequest = new UpdateChillerRequest
    {
      ChillerId = _deviceConfig.UniqueId,
      Config = laserConfig
    };

    await _chillerClient.UpdateChillerAsync(updateRequest);
  }

  public Task Activate()
    => _devicesClient.ActivateAsync(new DeviceActivateRequest
    {
      DeviceId = _deviceConfig.UniqueId
    }).ResponseAsync;

  public async Task Remove()
  {
    await _chillerClient.RemoveChillerAsync(new ChillerRequest { ChillerId = _deviceConfig.UniqueId });
    _messenger.Send(new DeviceDeletedMessage(_deviceConfig.UniqueId));
    await OnRemoveCallback();
  }

  public ChillerConfig ChillerConfig { get; }

  public Func<Task> OnRemoveCallback { get; }
  public LaserChillerConfigEditViewModel EditViewModel { get; }
}
