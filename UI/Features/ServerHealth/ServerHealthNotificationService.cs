using Ares.Services;
using UI.Application.Hosting;
using UI.Application.Notifications;

namespace UI.Features.ServerHealth;

/// <summary>
/// Uses the server health service to grab new state messages and publish them to the notification repo/service
/// </summary>
internal class ServerHealthNotificationService : ILocalService
{
  private readonly IAresUINotificationService _notificationService;
  private readonly ServerHealthService _serverHealthService;

  public ServerHealthNotificationService(ServerHealthService serverHealthService, IAresUINotificationService notificationService)
  {
    _serverHealthService = serverHealthService;
    _notificationService = notificationService;
  }

  public Task Start()
  {
    _serverHealthService.ServerStatus.Subscribe(ProcessServerState);
    return Task.CompletedTask;
  }

  private void ProcessServerState(ServerStatusResponse status)
  {
    switch(status.ServerStatus)
    {
      case ServerStatus.Idle:
      case ServerStatus.Busy:
        break;
      case ServerStatus.Error:
        _notificationService.PushNotification(new AresNotification
        {
          NotificationSeverity = Severity.Error,
          Message = status.StatusMessage,
          Title = "Error Processing Server State"
        });
        break;
      case ServerStatus.Stopping:
      case ServerStatus.Stopped:
        _notificationService.PushNotification(new AresNotification
        {
          NotificationSeverity = Severity.Warning,
          Message = status.StatusMessage,
          Title = "Server Has Been Stopped"
        });
        break;
      default:
        throw new ArgumentOutOfRangeException($"{status.ServerStatus} is out of range.");
    }
  }
}