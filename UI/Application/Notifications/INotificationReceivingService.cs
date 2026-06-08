using Ares.Services;

namespace UI.Application.Notifications;

public interface INotificationReceivingService
{
  void StartNotificationStream();
  // Might not be a bad idea to switch from directly accessing a datamodel AresNotification
  // to some sort of app-specific implementation so we're not depending on datamodel in our
  // Application layer
  void PushNotification(AresNotification notification);

  event Action<UiNotificationMessage>? OnNotificationReceived;
}

