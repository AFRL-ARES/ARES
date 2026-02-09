using Ares.Datamodel.Device;
using Ares.Services.Device;
using CommunityToolkit.Mvvm.Messaging;
using Grpc.Core;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using TubeFurnace.Config;
using TubeFurnace.Messaging;
using UI.Features.Devices.Shared;

namespace UI.Features.Devices.TubeFurnace
{
  public partial class TubeFurnaceSettingsViewModel : ReactiveObject
  {
    private readonly DeviceConfig _deviceConfig;
    private readonly AresDevices.AresDevicesClient _devicesClient;
    private readonly TubeFurnaceRpc.TubeFurnaceRpcClient _tubeFurnaceClient;
    private readonly IMessenger _messenger;

    public TubeFurnaceSettingsViewModel(DeviceConfig deviceConfig,
      TubeFurnaceRpc.TubeFurnaceRpcClient tubeFurnaceClient,
      AresDevices.AresDevicesClient devicesClient,
      IMessenger messenger,
      Func<Task> onRemoveCallback)
    {
      _deviceConfig = deviceConfig;
      TubeFurnaceConfig = deviceConfig.ConfigData.Unpack<TubeFurnaceConfig>();
      _tubeFurnaceClient = tubeFurnaceClient;
      _devicesClient = devicesClient;
      _messenger = messenger;
      OnRemoveCallback = onRemoveCallback;
      EditViewModel = new TubeFurnaceConfigEditViewModel(_tubeFurnaceClient, _devicesClient, TubeFurnaceConfig);
    }

    public TubeFurnaceConfig TubeFurnaceConfig { get; }

    public Func<Task> OnRemoveCallback { get; }

    public TubeFurnaceConfigEditViewModel EditViewModel { get; }

    public async Task<DeviceOperationalStatus> GetDeviceOperationalStatus()
    {
      try
      {
        var status = await _devicesClient.GetDeviceStatusAsync(new DeviceStatusRequest { DeviceId = _deviceConfig.UniqueId }).ResponseAsync;
        DeviceActive = status.OperationalState is OperationalState.Active;
        return status;
      }
      catch (RpcException)
      {
        return new DeviceOperationalStatus { OperationalState = OperationalState.Error, Message = $"Unable to find a registered stepper controller with a name {TubeFurnaceConfig.Name}" };
      }
    }

    public Task Activate()
      => _devicesClient.ActivateAsync(new DeviceActivateRequest { DeviceId = _deviceConfig.UniqueId }).ResponseAsync;

    public async Task Save()
    {
      var tubeFurnaceConfig = EditViewModel.Save();
      var updateRequest = new TubeFurnaceUpdateRequest
      {
        Id = _deviceConfig.UniqueId,
        Config = tubeFurnaceConfig
      };

      await _tubeFurnaceClient.UpdateTubeFurnaceAsync(updateRequest);
    }

    public async Task Remove()
    {
      await _tubeFurnaceClient.RemoveTubeFurnaceAsync(new TubeFurnaceRequest { TubeFurnaceId = _deviceConfig.UniqueId });
      _messenger.Send(new DeviceDeletedMessage(_deviceConfig.UniqueId));
      await OnRemoveCallback();
    }

    public async Task Init()
    {
      var status = await GetDeviceOperationalStatus();
      if (status.OperationalState != OperationalState.Active)
        return;

      var state = await _tubeFurnaceClient.GetStateAsync(new TubeFurnaceRequest { TubeFurnaceId = _deviceConfig.UniqueId });

    }

    [Reactive]
    public partial bool DeviceActive { get; private set; }
  }
}
