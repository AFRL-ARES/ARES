namespace Ares.Core.Notifications;

public interface INotificationHandler
{
  Task HandleNotification(string title, string message, NotificationSeverityEnum severity);
}
