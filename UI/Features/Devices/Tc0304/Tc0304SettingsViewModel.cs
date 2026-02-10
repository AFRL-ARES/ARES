using Ares.Datamodel.Device;
using Ares.Services.Device;
using CommunityToolkit.Mvvm.Messaging;
using Grpc.Core;
using ReactiveUI;
using Tc0304.Config;
using Tc0304.Services;
using UI.Application.Devices;
using UI.Features.Devices.Shared;

namespace UI.Features.Devices.Tc0304;

public class Tc0304SettingsViewModel : ReactiveObject
{
  private readonly TC0304Rpc.TC0304RpcClient _dataloggerClient;
  private readonly DeviceConfig _deviceConfig;
  private readonly AresDevices.AresDevicesClient _devicesClient;
  private readonly IMessenger _messenger;

  public Tc0304SettingsViewModel(DeviceConfig deviceConfig,
    TC0304Rpc.TC0304RpcClient dataloggerClient,
    AresDevices.AresDevicesClient devicesClient,
    IMessenger messenger,
    Func<Task> onRemoveCallback)
  {
    _deviceConfig = deviceConfig;
    DataloggerConfig = deviceConfig.ConfigData.Unpack<Tc0304Config>();
    _dataloggerClient = dataloggerClient;
    _devicesClient = devicesClient;
    _messenger = messenger;
    OnRemoveCallback = onRemoveCallback;
    EditViewModel = new Tc0304ConfigEditViewModel(_dataloggerClient, _devicesClient, DataloggerConfig);
  }

  public Tc0304Config DataloggerConfig { get; }

  public Func<Task> OnRemoveCallback { get; }

  public Tc0304ConfigEditViewModel EditViewModel { get; }

  public Task<DeviceOperationalStatus> GetDeviceOperationalStatus()
  {
    try
    {
      return _devicesClient.GetDeviceStatusAsync(new DeviceStatusRequest { DeviceId = _deviceConfig.UniqueId }).ResponseAsync;
    }
    catch(RpcException)
    {
      return Task.FromResult(new DeviceOperationalStatus { OperationalState = OperationalState.Error, Message = $"Unable to find a registered TC0304 datalogger with a name {DataloggerConfig.Name}" });
    }
  }



  public async Task Save()
  {
    var dataloggerConfig = EditViewModel.Save();
    var updateRequest = new UpdateTc0304Request
    {
      Id = _deviceConfig.UniqueId,
      Config = dataloggerConfig
    };

    await _dataloggerClient.UpdateTc0304Async(updateRequest);
  }

  public Task Activate()
    => _devicesClient.ActivateAsync(new DeviceActivateRequest
    {
      DeviceId = _deviceConfig.UniqueId
    }).ResponseAsync;

  public async Task Remove()
  {
    await _dataloggerClient.RemoveTc0304Async(new Tc0304Request { Tc0304Id = _deviceConfig.UniqueId });
    _messenger.Send(new DeviceDeletedMessage(_deviceConfig.UniqueId));
    await OnRemoveCallback();
  }
}
