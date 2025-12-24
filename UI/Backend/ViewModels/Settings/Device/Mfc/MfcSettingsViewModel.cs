using Ares.Alicat.Mfc.Config;
using Ares.Alicat.Mfc.Messaging;
using Ares.Datamodel.Device;
using Ares.Services.Device;
using CommunityToolkit.Mvvm.Messaging;
using DynamicData;
using Grpc.Core;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using UI.Backend.Devices;

namespace UI.Backend.ViewModels.Settings.Device.Mfc;

public partial class MfcSettingsViewModel : ReactiveObject
{
  private readonly DeviceConfig _deviceConfig;
  private readonly AresDevices.AresDevicesClient _devicesClient;
  private readonly MfcRpc.MfcRpcClient _mfcClient;
  private readonly IMessenger _messenger;
  private readonly ILogger<MfcSettingsViewModel> _logger;

  public MfcSettingsViewModel(DeviceConfig deviceConfig,
    MfcRpc.MfcRpcClient mfcClient,
    AresDevices.AresDevicesClient devicesClient,
    ILoggerFactory loggerFactory,
    IMessenger messenger,
    Func<Task> onRemoveCallback)
  {
    _deviceConfig = deviceConfig;
    MfcConfig = deviceConfig.ConfigData.Unpack<MfcConfig>();
    _mfcClient = mfcClient;
    _devicesClient = devicesClient;
    _messenger = messenger;
    OnRemoveCallback = onRemoveCallback;
    EditViewModel = new MfcConfigEditViewModel(_mfcClient, _devicesClient, MfcConfig, loggerFactory.CreateLogger<MfcConfigEditViewModel>());
    _logger = loggerFactory.CreateLogger<MfcSettingsViewModel>();
    SelectedSetpointSource = SetpointSource.UnknownSource;
  }

  public MfcConfig MfcConfig { get; }

  [Reactive]
  public partial bool DeviceActive { get; set; }

  public Func<Task> OnRemoveCallback { get; }

  public MfcConfigEditViewModel EditViewModel { get; }

  public IEnumerable<string>? AvailableGases { get; set; }
  public IEnumerable<char>? AvailableIds { get; set; }

  public char? CurrentId { get; set; }
  public string? CurrentGas { get; set; }

  public string? TargetGas { get; set; }
  public char? TargetId { get; set; }

  public SetpointSource[] AvailableSetpointSources { get; } =
    Enum.GetValues<SetpointSource>().Except([SetpointSource.UnknownSource]).ToArray();

  [Reactive]
  public partial SetpointSource SelectedSetpointSource { get; private set; }

  [Reactive]
  public partial bool SetpointSourceUpdating { get; private set; }

  public async Task Init()
  {
    var status = await GetDeviceOperationalStatus();
    if(status.OperationalState is not OperationalState.Active)
      return;

    var state = await _mfcClient.GetStateAsync(new DeviceRequest { DeviceId = _deviceConfig.UniqueId });
    AvailableGases = state.AvailableGasInfos.OrderBy(entry => entry.Index).Select(entry => entry.Name);
    var ids = await _mfcClient.GetAvailableIdsAsync(new GetAvailableIdsRequest { PortName = MfcConfig.PortName, Simulated = MfcConfig.Simulated });
    AvailableIds = ids.Ids.Select(s => s.First());
    CurrentGas = state.Data?.Gas;
    CurrentId = state.AssumedId?.FirstOrDefault();
    TargetId = CurrentId;

    await RefreshSetpointSource();
  }

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
      return new DeviceOperationalStatus { OperationalState = OperationalState.Error, Message = $"Unable to find a registered mfc with a name {MfcConfig.Name}" };
    }
  }

  public async Task Activate()
  {
    await _mfcClient.StartDataCaptureAsync(new DeviceRequest { DeviceId = _deviceConfig.UniqueId });
    await _devicesClient.ActivateAsync(new DeviceActivateRequest { DeviceId = _deviceConfig.UniqueId });
  }

  public async Task Save()
  {
    var mfcConfig = EditViewModel.Save();
    var updateRequest = new MfcUpdateRequest
    {
      Id = _deviceConfig.UniqueId,
      Config = mfcConfig
    };

    await _mfcClient.UpdateMfcAsync(updateRequest);
  }

  public async Task Remove()
  {
    await _mfcClient.RemoveMfcAsync(new MfcRemoveRequest { MfcId = _deviceConfig.UniqueId });
    _messenger.Send(new DeviceDeletedMessage(_deviceConfig.UniqueId));
    await OnRemoveCallback();
  }

  public async Task ChangeId()
  {
    if(!TargetId.HasValue || TargetId == CurrentId)
      return;

    await _mfcClient.ChangeHardwareUnitIdAsync(new ChangeUnitIdRequest { DeviceRequest = new DeviceRequest { DeviceId = _deviceConfig.UniqueId }, Id = TargetId.ToString() });
    MfcConfig.Id = TargetId.ToString();
    var updateRequest = new MfcUpdateRequest
    {
      Id = _deviceConfig.UniqueId,
      Config = MfcConfig
    };

    await _mfcClient.UpdateMfcAsync(updateRequest);
    await Init();
  }

  public async Task ChangeGas()
  {
    if(string.IsNullOrEmpty(TargetGas) || TargetGas == CurrentGas || AvailableGases is null)
      return;

    await _mfcClient.ChooseDifferentGasAsync(new ChooseDifferentGasRequest { DeviceRequest = new DeviceRequest { DeviceId = _deviceConfig.UniqueId }, GasNumber = AvailableGases.IndexOf(TargetGas) });
  }

  public async Task UpdateSetpointSource(SetpointSource source)
  {
    if (SelectedSetpointSource == source)
      return;

    if (MfcConfig.MfcType != MfcType.Basis2)
      return;

    SetpointSourceUpdating = true;
    try
    {
      await _mfcClient.SetSetpointSourceAsync(new SetSetpointSourceRequest { Id = _deviceConfig.UniqueId, Source = source });
      await RefreshSetpointSource();
    }
    catch (Exception e)
    {
      _logger.LogError(e, "Failed to update setpoint source for mfc {Mfc}", MfcConfig.Name);
      SelectedSetpointSource = SetpointSource.UnknownSource;
    }
    finally
    {
      SetpointSourceUpdating = false;
    }
  }

  private async Task RefreshSetpointSource()
  {
    try
    {
      var response = await _mfcClient.GetSetpointSourceAsync(new DeviceRequest { DeviceId = _deviceConfig.UniqueId });
      SelectedSetpointSource = response.Source;
    }
    catch (Exception e)
    {
      _logger.LogError(e, "Failed to load setpoint source for mfc {Mfc}", MfcConfig.Name);
      SelectedSetpointSource = SetpointSource.UnknownSource;
    }
  }
}
