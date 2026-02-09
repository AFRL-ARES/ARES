using Ares.Services;

namespace UI.Infrastructure.Notification;

public interface INotificationReceivingService
{
  void StartNotificationStream();
  void PushNotification(AresNotification notification);
}
