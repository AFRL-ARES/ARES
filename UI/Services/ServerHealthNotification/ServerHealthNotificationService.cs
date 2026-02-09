using Ares.Services;
using UI.Infrastructure.Interfaces;
using UI.Services.Notification;
using UI.Services.ServerHealth;

namespace UI.Services.ServerHealthNotification;

/// <summary>
/// Uses the server health service to grab new state messages and publish them to the notification repo/service
/// </summary>
internal class ServerHealthNotificationService : ILocalService
{
  private readonly IUiNotificationService _notificationService;
  private readonly ServerHealthService _serverHealthService;

  public ServerHealthNotificationService(ServerHealthService serverHealthService, IUiNotificationService notificationService)
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
        _notificationService.Notify(new UiNotificationMessage
        {
          Severity = UiNotificationSeverity.Error,
          Detail = status.StatusMessage
        });
        break;
      case ServerStatus.Stopping:
      case ServerStatus.Stopped:
        _notificationService.Notify(new UiNotificationMessage
        {
          Severity = UiNotificationSeverity.Warning,
          Detail = status.StatusMessage
        });
        break;
      default:
        throw new ArgumentOutOfRangeException($"{status.ServerStatus} is out of range.");
    }
  }
}
