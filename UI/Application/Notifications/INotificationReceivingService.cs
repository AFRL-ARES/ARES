using Ares.Services;

namespace UI.Application.Notifications;

/// <summary>
/// A service designated to receiving notifications from the core
/// </summary>
public interface INotificationReceivingService
{
  void StartNotificationStream();

  event Action<UiNotificationMessage>? OnNotificationReceived;
}

