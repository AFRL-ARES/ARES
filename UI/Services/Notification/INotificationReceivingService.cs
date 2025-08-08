using Ares.Services;

namespace UI.Services.Notification;

public interface INotificationReceivingService
{
  void StartNotificationStream();
  void PushNotification(AresNotification notification);
}
