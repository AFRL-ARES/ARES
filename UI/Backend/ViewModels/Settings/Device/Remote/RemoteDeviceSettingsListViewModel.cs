using Ares.Datamodel.Device;
using Ares.Services;
using Ares.Services.Device;
using Google.Protobuf.WellKnownTypes;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using UI.Services.Notification;

namespace UI.Backend.ViewModels.Settings.Device.Remote;

public class RemoteDeviceSettingsListViewModel : ReactiveObject
{
  private readonly AresDevices.AresDevicesClient _devicesClient;
  private readonly INotificationReceivingService _notificationService;
  public RemoteDeviceSettingsListViewModel(AresDevices.AresDevicesClient devicesClient, INotificationReceivingService notificationService)
  {
    _devicesClient = devicesClient;
    _notificationService = notificationService;
    _ = UpdateAvailableDevices();
  }

  public RemoteDeviceConfigEditViewModel GetNewConfigEditViewModel() => new();

  private async Task UpdateAvailableDevices()
  {
    SettingsViewModels = null;
    var remoteDevices = await _devicesClient.ListRemoteAresDevicesAsync(new Empty());
    UpdateViewModels(remoteDevices.Devices);
  }

  private void UpdateViewModels(IEnumerable<DeviceInfo> remoteDevices)
  {
    var viewModels = remoteDevices.Select(info => new RemoteDeviceSettingsViewModel(_devicesClient, _notificationService, info, OnDeviceRemoved)).ToArray();
    SettingsViewModels = viewModels;
  }

  public async Task AddNewRemoteDevice(RemoteDeviceConfig deviceConfig)
  {
    var request = new AddRemoteDeviceRequest() { Name = deviceConfig.Name, Url = deviceConfig.Url };
    var response = await _devicesClient.AddRemoteDeviceAsync(request);
    if(response.Success)
    {
      PushNotification(new AresNotification() { Message = $"Added new device {deviceConfig.Name}", NotificationSeverity = Severity.Success, Title = "Successfully Added Remote Device" });
      await UpdateAvailableDevices();
    }
    else
    {
      PushNotification(
        new AresNotification() { Message = $"Failed to add device {deviceConfig.Name}. {response.ErrorMessage}", NotificationSeverity = Severity.Error });
    }
  }

  private async Task OnDeviceRemoved()
  {
    SettingsViewModels = null;
    await UpdateAvailableDevices();
  }

  public void PushNotification(AresNotification notification) => _notificationService.PushNotification(notification);

  [Reactive]
  public IEnumerable<RemoteDeviceSettingsViewModel>? SettingsViewModels { get; private set; }
}
