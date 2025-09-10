using System.Collections.ObjectModel;
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
    SettingsViewModels = [];
    _ = UpdateAvailableDevices();
  }

  public RemoteDeviceConfigEditViewModel GetNewConfigEditViewModel() => new();

  private async Task UpdateAvailableDevices()
  {
    IsLoading = true;
    try
    {
      var remoteDevices = await _devicesClient.ListRemoteAresDevicesAsync(new Empty());
      UpdateViewModels(remoteDevices.Devices);
    }
    catch (Exception e)
    {
      PushNotification(new AresNotification(){Message = $"Could not retrieve remote devices. {e.Message}", Title = "Connection Error", NotificationSeverity = Severity.Error});
      SettingsViewModels.Clear();
    }
    finally
    {
      IsLoading = false;
    }
  }

  private void UpdateViewModels(IEnumerable<DeviceInfo> remoteDevices)
  {
    SettingsViewModels.Clear();
    var viewModels = remoteDevices.Select(info => new RemoteDeviceSettingsViewModel(_devicesClient, _notificationService, info, OnDeviceRemoved)).ToArray();
    foreach (var vm in viewModels)
    {
      SettingsViewModels.Add(vm);
    }
  }

  public async Task AddNewRemoteDevice(RemoteDeviceConfig deviceConfig)
  {
    try
    {
      var request = new AddRemoteDeviceRequest() { Name = deviceConfig.Name, Url = deviceConfig.Url };
      var response = await _devicesClient.AddRemoteDeviceAsync(request);
      if (response.Success)
      {
        PushNotification(new AresNotification() { Message = $"Added new device {deviceConfig.Name}", NotificationSeverity = Severity.Success, Title = "Successfully Added Remote Device" });
        await UpdateAvailableDevices();
      }
      else
      {
        PushNotification(
          new AresNotification() { Message = $"Failed to add device {deviceConfig.Name}. {response.ErrorMessage}", NotificationSeverity = Severity.Error, Title = "Error"});
      }
    }
    catch (Exception e)
    {
      PushNotification(new AresNotification(){Message = $"Failed to add device {deviceConfig.Name}. {e.Message}", Title = "Error", NotificationSeverity = Severity.Error});
    }
  }

  private async Task OnDeviceRemoved()
  {
    await UpdateAvailableDevices();
  }

  public void PushNotification(AresNotification notification) => _notificationService.PushNotification(notification);
  
  [Reactive] 
  public bool IsLoading { get; private set; }

  public ObservableCollection<RemoteDeviceSettingsViewModel> SettingsViewModels { get; }
}
