using Ares.Core.Device.Plugins.Drivers;
using Ares.Core.Grpc.Services;
using Ares.Datamodel.Device;
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
  private readonly INotificationReceivingService _notificationService;

  public PluginDeviceSettingsViewModel(DeviceConfig deviceConfig, 
    DeviceDriver driver, 
    DevicesService devicesService, 
    INotificationReceivingService notificationService,
    Func<Task> onRemoveCallback)
  {
    _deviceConfig = deviceConfig;
    _devicesService = devicesService;
    _notificationService = notificationService;
    Name = _deviceConfig.DeviceName;
    Id = _deviceConfig.DeviceId;

    EditViewModel = new PluginDeviceConfigEditViewModel(_deviceConfig, driver, false, devicesService);

    SaveCommand = ReactiveCommand.CreateFromTask(SaveAsync);
    RemoveCommand = ReactiveCommand.CreateFromTask(() => RemoveAsync(onRemoveCallback));
    FetchSettingsCommand = ReactiveCommand.CreateFromTask(FetchSettingsAsync);
    PushSettingsCommand = ReactiveCommand.CreateFromTask(PushSettingsAsync);
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
      PushNotification(new AresNotification
      {
        Title = "Device Update",
        Message = $"Device {deviceConfig.DeviceName} updated successfully.",
        NotificationSeverity = Severity.Success
      });

    }

    else
    {
      PushNotification(new AresNotification
      {
        Title = "Device Update Failed",
        Message = $"Device {deviceConfig.DeviceName} failed to update: {response.ErrorMessage}",
        NotificationSeverity = Severity.Error
      });
    }
  }

  private async Task RemoveAsync(Func<Task> onRemoveCallback)
  {
    var request = new RemoveDeviceRequest() { DeviceId = _deviceConfig.UniqueId };
    await _devicesService.RemoveAresDevice(request, null);
    await onRemoveCallback();
  }

  private async Task FetchSettingsAsync()
  {

  }

  private async Task PushSettingsAsync()
  {

  }

  private void PushNotification(AresNotification notification) => _notificationService.PushNotification(notification);

  public PluginDeviceConfigEditViewModel EditViewModel { get; }

  public ReactiveCommand<Unit, Unit> SaveCommand { get; }
  public ReactiveCommand<Unit, Unit> RemoveCommand { get; }
  public ReactiveCommand<Unit, Unit> FetchSettingsCommand { get; }
  public ReactiveCommand<Unit, Unit> PushSettingsCommand { get; }

  [Reactive]
  public partial string Name { get; private set; }

  [Reactive]
  public partial string Id { get; private set; }
}
