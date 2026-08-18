using Ares.Core.Device.Plugins.Drivers;
using Ares.Core.Device.Providers;
using Ares.Core.Grpc.Services;
using Ares.Datamodel;
using Ares.Datamodel.Device;
using Ares.Device;
using Ares.Services;
using Ares.Services.Device;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System.Reactive;
using UI.Application.Notifications;

namespace UI.Features.Devices.Plugin;

public partial class PluginDeviceSettingsViewModel : ReactiveObject
{
  private readonly DeviceConfig _deviceConfig;
  private readonly DevicesService _devicesService;
  private readonly IAresDeviceProvider _deviceProvider;
  private readonly IUiNotificationService _notificationService;

  public PluginDeviceSettingsViewModel(DeviceConfig deviceConfig, 
    DeviceDriver driver, 
    DevicesService devicesService,
    IAresDeviceProvider deviceProvider,
    IUiNotificationService notificationService,
    Func<Task> onRemoveCallback)
  {
    _deviceConfig = deviceConfig;
    _devicesService = devicesService;
    _notificationService = notificationService;
    _deviceProvider = deviceProvider;
    Name = _deviceConfig.DeviceName;
    Id = _deviceConfig.DeviceId;

    Device = _deviceProvider.GetDevice(Id);
    Description = Device?.Description ?? string.Empty;
    SettingsSchema = Device?.SettingSchema ?? new AresStructSchema();
    Settings = new AresStruct();
    EditViewModel = new PluginDeviceConfigEditViewModel(_deviceConfig, driver, false, devicesService);

    SaveCommand = ReactiveCommand.CreateFromTask(SaveAsync);
    RemoveCommand = ReactiveCommand.CreateFromTask(() => RemoveAsync(onRemoveCallback));
  }

  private async Task SaveAsync()
  {
    var deviceConfig = EditViewModel.Save();
    var request = new UpdateDeviceRequest()
    {
      DeviceId = _deviceConfig.UniqueId,
      UpdatedConfig = deviceConfig
    };

    var response = await _devicesService.UpdateAresDevice(request, null);

    if(response.Success)
    {
      PushNotification(new UiNotificationMessage
      {
        Summary = "Device Update",
        Detail = $"Device {deviceConfig.DeviceName} updated successfully.",
        Severity = UiNotificationSeverity.Success
      });

    }

    else
    {
      PushNotification(new UiNotificationMessage
      {
        Summary = "Device Update Failed",
        Detail = $"Device {deviceConfig.DeviceName} failed to update: {response.ErrorMessage}",
        Severity = UiNotificationSeverity.Error
      });
    }
  }

  private async Task RemoveAsync(Func<Task> onRemoveCallback)
  {
    var request = new RemoveDeviceRequest() { DeviceId = _deviceConfig.UniqueId };
    await _devicesService.RemoveAresDevice(request, null);
    await onRemoveCallback();
  }

  public async Task FetchSettingsAsync()
  {
    if(Device is not null)
      Settings = await Device.GetSettings();

    else
      Settings = new AresStruct();
  }

  public async Task PushSettingsAsync()
  {
    try
    {
      if(Device is not null)
        await Device.UpdateSettings(Settings);

      var successNotification = new UiNotificationMessage
      {
        Summary = "Update Device Settings",
        Detail = $"ARES successfully updated the settings for {Device?.Name}!",
        Severity = UiNotificationSeverity.Success
      };

      _notificationService.Notify(successNotification);
    }

    catch(Exception ex)
    {
      var failedNotification = new UiNotificationMessage
      {
        Summary = "Failed to Update Settings",
        Detail = $"ARES failed to update the settings for {Device?.Name}. Reason: {ex.Message}", 
        Severity = UiNotificationSeverity.Warning 
      };

      _notificationService.Notify(failedNotification);
    }
  }

  public AresValue? GetMatchingSettingValue(string key)
  => Settings?.Fields.FirstOrDefault(f => f.Key == key).Value ?? null;

  private void PushNotification(UiNotificationMessage notification) => _notificationService.Notify(notification);

  public PluginDeviceConfigEditViewModel EditViewModel { get; }

  public ReactiveCommand<Unit, Unit> SaveCommand { get; }
  public ReactiveCommand<Unit, Unit> RemoveCommand { get; }

  public IAresDevice? Device { get; set; }

  [Reactive]
  public partial string Name { get; private set; }

  [Reactive]
  public partial string Id { get; private set; }

  [Reactive]
  public partial string Description { get; set; }

  [Reactive]
  public partial AresStructSchema SettingsSchema { get; private set; }

  [Reactive]
  public partial AresStruct Settings { get; set; }

}
