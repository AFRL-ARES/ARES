using Ares.Datamodel.Device;
using Ares.Core.Grpc.Services;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using UI.Application.Notifications;

namespace UI.Features.Devices.Plugin;

public partial class PluginDeviceSettingsViewModel: ReactiveObject
{
  private readonly DeviceConfig _deviceConfig;
  private readonly DevicesService _devicesClient;
  private readonly INotificationReceivingService _notificationService;
  public PluginDeviceSettingsViewModel(DeviceConfig deviceConfig,
  DevicesService devicesClient,
  INotificationReceivingService notificationService)
  {
    _deviceConfig = deviceConfig;
    _devicesClient = devicesClient;
    _notificationService = notificationService;

    Name = _deviceConfig.DeviceName;
    Id = _deviceConfig.UniqueId;
    DriverName = _deviceConfig.DriverName;
  }

  [Reactive]
  public partial string Name { get; private set; }

  [Reactive]
  public partial string Id { get; private set; }

  [Reactive]
  public partial string DriverName { get; private set; }
}
