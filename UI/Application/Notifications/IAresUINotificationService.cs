using Ares.Services;

namespace UI.Application.Notifications;

/// <summary>
/// A service designated to receiving notifications from the core
/// </summary>
public interface IAresUINotificationService
{
  void StartNotificationStream();

  event Action<AresNotification>? OnNotificationReceived;

  void PushNotification(AresNotification notification);
}