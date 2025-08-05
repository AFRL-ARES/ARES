using Ares.Messaging;

namespace UI.Services.Notification;

public interface INotificationReceivingService
{
  void StartNotificationStream();
  void PushNotification(AresNotification notification);
}
