using UI.Features.Devices.Remote;
using UI.Application.Notifications;
using UI.Infrastructure.Devices;
using UI.Application.Devices.Repos;

namespace UI;

public class ServiceStarter : IHostedService
{
  private readonly INotificationReceivingService _notificationReceivingService;
  private readonly IDeviceControlViewModelRepo _deviceControlViewModelRepo;
  private readonly DeviceAdapterManager _deviceAdapterManager;
  private readonly RemoteDeviceControlViewModelFactory _remoteDeviceViewModelFactory;
  private readonly DeviceDriverSyncManager _deviceDriverSyncManager;

  public ServiceStarter(
    INotificationReceivingService notificationReceivingService,
    IServiceProvider serviceProvider,
    IDeviceControlViewModelRepo deviceControlViewModelRepo,
    DeviceAdapterManager deviceAdapterManager,
    RemoteDeviceControlViewModelFactory remoteDeviceViewModelFactory,
    DeviceDriverSyncManager deviceDriverSyncManager)
  {
    _notificationReceivingService = notificationReceivingService;
    _deviceControlViewModelRepo = deviceControlViewModelRepo;
    _deviceAdapterManager = deviceAdapterManager;
    _remoteDeviceViewModelFactory = remoteDeviceViewModelFactory;
    _deviceDriverSyncManager = deviceDriverSyncManager;
  }

  public async Task StartAsync(CancellationToken cancellationToken)
  {
    await _deviceDriverSyncManager.SyncDriversAsync();
    _notificationReceivingService.StartNotificationStream();
    _deviceControlViewModelRepo.Initialize();
    _deviceAdapterManager.Activate();
    _remoteDeviceViewModelFactory.Start(TimeSpan.FromSeconds(5));
  }

  public async Task StopAsync(CancellationToken cancellationToken)
  {
    _deviceControlViewModelRepo.Dispose();
    await _deviceAdapterManager.DisposeAsync();
    await _remoteDeviceViewModelFactory.DisposeAsync();
  }
}

