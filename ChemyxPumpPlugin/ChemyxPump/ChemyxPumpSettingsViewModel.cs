using Ares.Datamodel.Device;
using Ares.Services.Device;
using ChemyxPumpPlugin.Config;
using ChemyxPumpPlugin.Services;
using CommunityToolkit.Mvvm.Messaging;
using Grpc.Core;
using ReactiveUI;
using UI.Application.Devices;
using UI.Features.Devices.Shared;

namespace UI.Features.Devices.ChemyxPump;

public class ChemyxPumpSettingsViewModel : ReactiveObject
{
  private readonly ChemyxPumpRpc.ChemyxPumpRpcClient _pumpClient;
  private readonly DeviceConfig _deviceConfig;
  private readonly AresDevices.AresDevicesClient _devicesClient;
  private readonly IMessenger _deviceDeletionMessenger;
  public ChemyxPumpSettingsViewModel(DeviceConfig deviceConfig,
    ChemyxPumpRpc.ChemyxPumpRpcClient pumpClient,
    AresDevices.AresDevicesClient devicesClient,
    IMessenger deviceDeletionMessenger,
    Func<Task> onRemoveCallback)
  {
    _deviceConfig = deviceConfig;
    _pumpClient = pumpClient;
    _devicesClient = devicesClient;
    _deviceDeletionMessenger = deviceDeletionMessenger;
    PumpConfig = deviceConfig.ConfigData.Unpack<ChemyxPumpConfig>();
    OnRemoveCallback = onRemoveCallback;
    EditViewModel = new ChemyxPumpConfigEditViewModel(_pumpClient, _devicesClient, PumpConfig);
  }

  public ChemyxPumpConfig PumpConfig { get; }
  public Func<Task> OnRemoveCallback { get; }
  public ChemyxPumpConfigEditViewModel EditViewModel { get; }

  public Task<DeviceOperationalStatus> GetDeviceOperationalStatus()
  {
    try
    {
      return _devicesClient.GetDeviceStatusAsync(new DeviceStatusRequest { DeviceId = _deviceConfig.UniqueId }).ResponseAsync;
    }

    catch(RpcException)
    {
      return Task.FromResult(new DeviceOperationalStatus { OperationalState = OperationalState.Error, Message = $"Unable to find a registered Chemyx Pump with a name {PumpConfig.Name}" });
    }
  }

  public async Task Save()
  {
    var pumpConfig = EditViewModel.Save();
    await _pumpClient.UpdatePumpAsync(new UpdatePumpRequest { Id = _deviceConfig.UniqueId, Config = pumpConfig });
  }

  public Task Activate()
  => _devicesClient.ActivateAsync(new DeviceActivateRequest
  {
    DeviceId = _deviceConfig.UniqueId
  }).ResponseAsync;

  public async Task Remove()
  {
    await _pumpClient.RemoveChemyxPumpAsync(new PumpRequest { DeviceId = _deviceConfig.UniqueId });
    _deviceDeletionMessenger.Send(new DeviceDeletedMessage(_deviceConfig.UniqueId));
    await OnRemoveCallback();
  }
}
