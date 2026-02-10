namespace UI.Domain.Notifications;

public interface IUiNotificationService
{
  void Notify(UiNotificationMessage message);
}
