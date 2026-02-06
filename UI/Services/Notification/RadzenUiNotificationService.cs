using Radzen;

namespace UI.Services.Notification;

internal sealed class RadzenUiNotificationService : IUiNotificationService
{
  private readonly NotificationService _notificationService;

  public RadzenUiNotificationService(NotificationService notificationService)
  {
    _notificationService = notificationService;
  }

  public void Notify(UiNotificationMessage message)
  {
    var radzenNotification = new NotificationMessage
    {
      Summary = message.Summary,
      Detail = message.Detail,
      Severity = ConvertSeverity(message.Severity),
      Duration = message.DurationMs,
      CloseOnClick = message.CloseOnClick
    };

    _notificationService.Notify(radzenNotification);
  }

  private static NotificationSeverity ConvertSeverity(UiNotificationSeverity severity)
  {
    return severity switch
    {
      UiNotificationSeverity.Error => NotificationSeverity.Error,
      UiNotificationSeverity.Success => NotificationSeverity.Success,
      UiNotificationSeverity.Warning => NotificationSeverity.Warning,
      _ => NotificationSeverity.Info
    };
  }
}
