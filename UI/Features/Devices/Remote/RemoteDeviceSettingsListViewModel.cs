using Ares.Datamodel.Device;
using Ares.Services;
using Ares.Services.Device;
using Ares.Core.Grpc.Services;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System.Collections.ObjectModel;
using UI.Application.Notifications;
using Google.Protobuf.WellKnownTypes;

namespace UI.Features.Devices.Remote;

public partial class RemoteDeviceSettingsListViewModel : ReactiveObject
{
  private readonly IUiNotificationService _notificationService;
  private readonly DevicesService _devicesService;

  public RemoteDeviceSettingsListViewModel(DevicesService devicesService, IUiNotificationService notificationService)
  {
    _notificationService = notificationService;
    SettingsViewModels = [];
    _devicesService = devicesService;
    _ = UpdateAvailableDevices();
  }

  public RemoteDeviceConfigEditViewModel GetNewConfigEditViewModel() => new();

  private async Task UpdateAvailableDevices()
  {
    IsLoading = true;
    try
    {
      var remoteDevices = await _devicesService.ListRemoteAresDevices(new Empty(), null); 
      UpdateViewModels(remoteDevices.Devices);
    }
    catch (Exception e)
    {
      PushNotification(new UiNotificationMessage { Detail = $"Could not retrieve remote devices. {e.Message}", Summary = "Connection Error", Severity = UiNotificationSeverity.Error });
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
    var viewModels = remoteDevices.Select(info => new RemoteDeviceSettingsViewModel(_devicesService, _notificationService, info, OnDeviceRemoved)).ToArray();
    foreach (var vm in viewModels)
    {
      SettingsViewModels.Add(vm);
    }
  }

  public async Task AddNewRemoteDevice(RemoteDeviceConfig deviceConfig)
  {
    try
    {
      var request = new AddRemoteDeviceRequest { Name = deviceConfig.Name, Url = deviceConfig.Url };
      var response = await _devicesService.AddRemoteDevice(request, null);
      if (response.Success)
      {
        PushNotification(new UiNotificationMessage { Detail = $"Added new device {deviceConfig.Name}", Severity = UiNotificationSeverity.Success, Summary = "Successfully Added Remote Device" });
        await UpdateAvailableDevices();
      }
      else
      {
        PushNotification(new UiNotificationMessage { Detail = $"Failed to add device {deviceConfig.Name}. {response.ErrorMessage}", Severity = UiNotificationSeverity.Error, Summary = "Error Adding Device"});
      }
    }
    catch (Exception e)
    {
      PushNotification(new UiNotificationMessage { Detail = $"Failed to add device {deviceConfig.Name}. {e.Message}", Summary = "Error Adding Device", Severity = UiNotificationSeverity.Error });
    }
  }

  private async Task OnDeviceRemoved()
  {
    await UpdateAvailableDevices();
  }

  public void PushNotification(UiNotificationMessage notification) => _notificationService.Notify(notification);
  
  [Reactive] 
  public partial bool IsLoading { get; private set; }

  public ObservableCollection<RemoteDeviceSettingsViewModel> SettingsViewModels { get; }
}


