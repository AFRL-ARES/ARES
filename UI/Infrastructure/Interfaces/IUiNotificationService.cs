using UI.Infrastructure.Notification;

namespace UI.Infrastructure.Interfaces;

public interface IUiNotificationService
{
  void Notify(UiNotificationMessage message);
}
