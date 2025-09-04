using UI.Backend.Devices;
using UI.Services.Notification;

namespace UI;

public class ServiceStarter : IHostedService
{
  private readonly INotificationReceivingService _notificationReceivingService;
  private readonly DeviceAdapterManager _deviceAdapterManager;

  public ServiceStarter(
    INotificationReceivingService notificationReceivingService,
    IServiceProvider serviceProvider,
    DeviceAdapterManager deviceAdapterManager)
  {
    _notificationReceivingService = notificationReceivingService;
    _deviceAdapterManager = deviceAdapterManager;
  }

  public Task StartAsync(CancellationToken cancellationToken)
  {
    _notificationReceivingService.StartNotificationStream();
    _deviceAdapterManager.Activate();
    return Task.CompletedTask;
  }

  public Task StopAsync(CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }
}