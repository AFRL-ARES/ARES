using Ares.Alicat.Mfc.Config;
using Ares.Alicat.Mfc.Messaging;
using Ares.Datamodel.Device;
using Ares.Services.Device;
using DynamicData;
using Grpc.Core;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace UI.Backend.ViewModels.Settings.Device.Mfc;

public class MfcSettingsViewModel : ReactiveObject
{
  private readonly DeviceConfig _deviceConfig;
  private readonly AresDevices.AresDevicesClient _devicesClient;
  private readonly MfcRpc.MfcRpcClient _mfcClient;

  public MfcSettingsViewModel(DeviceConfig deviceConfig,
    MfcRpc.MfcRpcClient mfcClient,
    AresDevices.AresDevicesClient devicesClient,
    Func<Task> onRemoveCallback)
  {
    _deviceConfig = deviceConfig;
    MfcConfig = deviceConfig.ConfigData.Unpack<MfcConfig>();
    _mfcClient = mfcClient;
    _devicesClient = devicesClient;
    OnRemoveCallback = onRemoveCallback;
    EditViewModel = new MfcConfigEditViewModel(_mfcClient, _devicesClient, MfcConfig);
  }

  public MfcConfig MfcConfig { get; }

  [Reactive]
  public bool DeviceActive { get; set; }

  public Func<Task> OnRemoveCallback { get; }

  public MfcConfigEditViewModel EditViewModel { get; }

  public IEnumerable<string>? AvailableGases { get; set; }
  public IEnumerable<char>? AvailableIds { get; set; }

  public char? CurrentId { get; set; }
  public string? CurrentGas { get; set; }

  public string? TargetGas { get; set; }
  public char? TargetId { get; set; }

  public async Task Init()
  {
    var status = await GetDeviceStatus();
    if (status.DeviceState is not DeviceState.Active)
      return;

    var deviceState = await _mfcClient.GetStateAsync(new DeviceRequest { DeviceName = MfcConfig.Name });
    AvailableGases = deviceState.AvailableGasInfos.OrderBy(entry => entry.Index).Select(entry => entry.Name);
    var ids = await _mfcClient.GetAvailableIdsAsync(new GetAvailableIdsRequest { PortName = MfcConfig.PortName, Simulated = MfcConfig.Simulated });
    AvailableIds = ids.Ids.Select(s => s.First());
    CurrentGas = deviceState.Data?.Gas;
    CurrentId = deviceState.AssumedId?.FirstOrDefault();
    TargetId = CurrentId;
  }

  public async Task<DeviceStatus> GetDeviceStatus()
  {
    try
    {
      var status = await _devicesClient.GetDeviceStatusAsync(new DeviceStatusRequest { DeviceName = MfcConfig.Name }).ResponseAsync;
      DeviceActive = status.DeviceState is DeviceState.Active;
      return status;
    }
    catch (RpcException)
    {
      return new DeviceStatus { DeviceState = DeviceState.Error, Message = $"Unable to find a registered mfc with a name {MfcConfig.Name}" };
    }
  }

  public async Task Activate()
  {
    await _mfcClient.StartDataCaptureAsync(new DeviceRequest { DeviceName = MfcConfig.Name });
    await _devicesClient.ActivateAsync(new DeviceActivateRequest { DeviceName = MfcConfig.Name });
  }

  public async Task Save()
  {
    var mfcConfig = EditViewModel.Save();
    await _mfcClient.UpdateMfcAsync(mfcConfig);
  }

  public async Task Remove()
  {
    await _mfcClient.RemoveMfcAsync(new MfcRemoveRequest { MfcName = _deviceConfig.DeviceName });
    await OnRemoveCallback();
  }

  public async Task ChangeId()
  {
    if (!TargetId.HasValue || TargetId == CurrentId)
      return;

    await _mfcClient.ChangeHardwareUnitIdAsync(new ChangeUnitIdRequest { DeviceRequest = new DeviceRequest { DeviceName = MfcConfig.Name }, Id = TargetId.ToString() });
    MfcConfig.Id = TargetId.ToString();
    await _mfcClient.UpdateMfcAsync(MfcConfig);
    await Init();
  }

  public async Task ChangeGas()
  {
    if (string.IsNullOrEmpty(TargetGas) || TargetGas == CurrentGas || AvailableGases is null)
      return;

    await _mfcClient.ChooseDifferentGasAsync(new ChooseDifferentGasRequest { DeviceRequest = new DeviceRequest { DeviceName = MfcConfig.Name }, GasNumber = AvailableGases.IndexOf(TargetGas) });
  }
}
