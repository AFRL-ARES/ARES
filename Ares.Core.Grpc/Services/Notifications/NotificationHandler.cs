using Ares.Core.Notifications;
using Ares.Services;
using Google.Protobuf.WellKnownTypes;
using System;
using System.Threading.Tasks;

namespace Ares.Core.Grpc.Services.Notifications;

public class NotificationHandler : INotificationHandler
{
  private readonly AresNotificationService _notificationService;

  public NotificationHandler(AresNotificationService notificationService)
  {
    _notificationService = notificationService;
  }

  public async Task HandleNotification(string title, string message, NotificationSeverityEnum severity, bool loiter = false)
  {
    var notification = new AresNotification
    {
      Title = title,
      Message = message,
      NotificationSeverity = NotificationSeverityConverter(severity),
      Timestamp = DateTime.UtcNow.ToTimestamp(),
      Loiter = loiter,
      UniqueId = Guid.NewGuid().ToString()
    };

    await _notificationService.SendNotification(notification);
  }

  private Severity NotificationSeverityConverter(NotificationSeverityEnum severity)
  {
    switch(severity)
    {
      case NotificationSeverityEnum.Info:
        return Severity.Info;
      case NotificationSeverityEnum.Warning:
        return Severity.Warning;
      case NotificationSeverityEnum.Error:
        return Severity.Error;
      case NotificationSeverityEnum.Danger:
        return Severity.Danger;
      case NotificationSeverityEnum.Success:
        return Severity.Success;
      default:
        return Severity.Unspecified;
    }
  }
}