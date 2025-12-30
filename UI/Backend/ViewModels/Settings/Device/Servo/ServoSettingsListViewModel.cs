using Ares.Datamodel.Device;
using Ares.Services;
using Ares.Services.Device;
using CommunityToolkit.Mvvm.Messaging;
using HerkulexDRS;
using HerkulexDRS.Config;
using HerkulexDRS.Services;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using UI.Services.Notification;

namespace UI.Backend.ViewModels.Settings.Device.Servo;

public partial class ServoSettingsListViewModel : ReactiveObject
{
  private readonly HerkulexDRSRpc.HerkulexDRSRpcClient _servoClient;
  private readonly AresDevices.AresDevicesClient _devicesClient;
  private readonly INotificationReceivingService _notificationService;
  private readonly IMessenger _messenger;

  public ServoSettingsListViewModel(AresDevices.AresDevicesClient devicesClient, 
    HerkulexDRSRpc.HerkulexDRSRpcClient servoClient, 
    INotificationReceivingService notificationService, 
    IMessenger messenger)
  {
    _servoClient = servoClient;
    _devicesClient = devicesClient;
    _notificationService = notificationService;
    _messenger = messenger;
    UpdateConfigs();
  }

  [Reactive]
  public partial IEnumerable<ServoSettingsViewModel>? SettingsViewModels { get; private set; }

  private void UpdateViewModels(IEnumerable<DeviceConfig> deviceConfigs)
  {
    var viewModels = deviceConfigs.Select(config => new ServoSettingsViewModel(config, _servoClient, _devicesClient, _messenger, OnConfigRemoved));
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
    var notification = new AresNotification()
    {
      Title = $"Successfully Added Servo {config.Name}",
      Message = "ARES successfully added a new servo device, and it is now active.",
      NotificationSeverity = Severity.Success
    };
    _notificationService.PushNotification(notification);
  }
}
