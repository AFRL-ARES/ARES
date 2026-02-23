using System.Collections.ObjectModel;
using Ares.Datamodel.Device;
using Ares.Services;
using Ares.Services.Device;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using UI.Application.Notifications;

namespace UI.Features.Devices.Plugin;

public partial class PluginDeviceSettingsListViewModel(AresDevices.AresDevicesClient _devicesClient, INotificationReceivingService _notificationService) : ReactiveObject
{
    public async Task Initialize(string driverName)
    {
        DriverName = driverName;
        await UpdateAvailableDevices();
    }

    private async Task UpdateAvailableDevices()
    {
        IsLoading = true;
        try
        {
            var request = new DeviceConfigRequest { DeviceType = DriverName };
            var response = await _devicesClient.GetAllDeviceConfigsAsync(request);
            UpdateViewModels(response.Configs);
        }
        catch (Exception e)
        {
            _notificationService.PushNotification(new AresNotification
            {
                Message = $"Could not retrieve devices for {DriverName}. {e.Message}",
                Title = "Connection Error",
                NotificationSeverity = Severity.Error
            });
            SettingsViewModels.Clear();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void UpdateViewModels(IEnumerable<DeviceConfig> configs)
    {
        SettingsViewModels.Clear();
        foreach (var config in configs)
        {
            SettingsViewModels.Add(new PluginDeviceSettingsViewModel(config, _devicesClient, _notificationService));
        }
    }

  [Reactive]
  public partial string DriverName { get; set; }

  public ObservableCollection<PluginDeviceSettingsViewModel> SettingsViewModels { get; } = [];

  [Reactive]
  public partial bool IsLoading { get; private set; }
}