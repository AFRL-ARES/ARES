using Ares.Messaging.Device;
using Grpc.Core;
using ReactiveUI;
using Tc0304.Config;
using Tc0304.Services;

namespace UI.Backend.ViewModels.Settings.Device.Tc0304;

public class Tc0304SettingsViewModel : ReactiveObject
{
  private readonly TC0304Rpc.TC0304RpcClient _dataloggerClient;
  private readonly DeviceConfig _deviceConfig;
  private readonly AresDevices.AresDevicesClient _devicesClient;

  public Tc0304SettingsViewModel(DeviceConfig deviceConfig,
    TC0304Rpc.TC0304RpcClient dataloggerClient,
    AresDevices.AresDevicesClient devicesClient,
    Func<Task> onRemoveCallback)
  {
    _deviceConfig = deviceConfig;
    DataloggerConfig = deviceConfig.ConfigData.Unpack<Tc0304Config>();
    _dataloggerClient = dataloggerClient;
    _devicesClient = devicesClient;
    OnRemoveCallback = onRemoveCallback;
    EditViewModel = new Tc0304ConfigEditViewModel(_dataloggerClient, _devicesClient, DataloggerConfig);
  }

  public Tc0304Config DataloggerConfig { get; }

  public Func<Task> OnRemoveCallback { get; }

  public Tc0304ConfigEditViewModel EditViewModel { get; }

  public Task<DeviceStatus> GetDeviceStatus()
  {
    try
    {
      return _devicesClient.GetDeviceStatusAsync(new DeviceStatusRequest { DeviceName = DataloggerConfig.Name }).ResponseAsync;
    }
    catch (RpcException)
    {
      return Task.FromResult(new DeviceStatus { DeviceState = DeviceState.Error, Message = $"Unable to find a registered TC0304 datalogger with a name {DataloggerConfig.Name}" });
    }
  }



  public async Task Save()
  {
    var dataloggerConfig = EditViewModel.Save();
    await _dataloggerClient.UpdateTc0304Async(dataloggerConfig);
  }

  public Task Activate()
    => _devicesClient.ActivateAsync(new DeviceActivateRequest
    {
      DeviceName = DataloggerConfig.Name
    }).ResponseAsync;

  public async Task Remove()
  {
    await _dataloggerClient.RemoveTc0304Async(new Tc0304Request { Tc0304Name = _deviceConfig.DeviceName });
    await OnRemoveCallback();
  }
}
