using Ares.Services;

namespace UI.Domain.Notifications;

public interface INotificationReceivingService
{
  void StartNotificationStream();
  void PushNotification(AresNotification notification);
}
