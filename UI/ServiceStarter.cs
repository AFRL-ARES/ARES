using UI.Services.Notification;

namespace UI;

public class ServiceStarter : IHostedService
{
  private readonly INotificationReceivingService _notificationReceivingService;

  public ServiceStarter(INotificationReceivingService notificationReceivingService, IServiceProvider serviceProvider)
  {
    _notificationReceivingService = notificationReceivingService;
  }

  public Task StartAsync(CancellationToken cancellationToken)
  {
    _notificationReceivingService.StartNotificationStream();
    return Task.CompletedTask;
  }

  public Task StopAsync(CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }
}