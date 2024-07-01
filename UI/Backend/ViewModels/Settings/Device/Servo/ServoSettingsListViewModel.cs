using Ares.Messaging.Device;
using HerkulexDRS;
using HerkulexDRS.Config;
using HerkulexDRS.Services;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace UI.Backend.ViewModels.Settings.Device.Servo;

public class ServoSettingsListViewModel : ReactiveObject
{
  private readonly HerkulexDRSRpc.HerkulexDRSRpcClient _servoClient;
  private readonly AresDevices.AresDevicesClient _devicesClient;
  public ServoSettingsListViewModel(AresDevices.AresDevicesClient devicesClient, HerkulexDRSRpc.HerkulexDRSRpcClient servoClient)
  {
    _servoClient = servoClient;
    _devicesClient = devicesClient;
    UpdateConfigs();
  }

  [Reactive]
  public IEnumerable<ServoSettingsViewModel>? SettingsViewModels { get; private set; }

  private void UpdateViewModels(IEnumerable<DeviceConfig> deviceConfigs)
  {
    var viewModels = deviceConfigs.Select(config => new ServoSettingsViewModel(config, _servoClient, _devicesClient, OnConfigRemoved));
    SettingsViewModels = viewModels;
  }

  public ServoConfigEditViewModel GetNewConfigEditViewModel()
    => new(_servoClient, _devicesClient);

  private Task UpdateConfigs()
  {
    SettingsViewModels = null;
    return _devicesClient
      .GetAllDeviceConfigsAsync(new DeviceConfigRequest { DeviceType = typeof(IServo).FullName })
      .ResponseAsync.ContinueWith(task => UpdateViewModels(task.Result.Configs));
  }

  private async Task OnConfigRemoved()
  {
    SettingsViewModels = null;
    await UpdateConfigs();
  }

  public async Task AddNewConfig(ServoConfig config)
  {
    await _servoClient.AddServoAsync(config);
    await UpdateConfigs();
  }
}
