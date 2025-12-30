using Ares.Services;

namespace UI.Services.Notification;

public interface INotificationReceivingService
{
  void PushNotification(AresNotification notification);
  Task InitializeAsync();
}
