namespace UI.Domain.Notifications;

public static class UiNotificationServiceExtensions
{
  public static void Success(this IUiNotificationService notificationService, string detail, string summary = "")
  {
    notificationService.Notify(new UiNotificationMessage
    {
      Summary = summary,
      Detail = detail,
      Severity = UiNotificationSeverity.Success
    });
  }

  public static void Error(this IUiNotificationService notificationService, string detail, string summary = "")
  {
    notificationService.Notify(new UiNotificationMessage
    {
      Summary = summary,
      Detail = detail,
      Severity = UiNotificationSeverity.Error
    });
  }

  public static void Warning(this IUiNotificationService notificationService, string detail, string summary = "")
  {
    notificationService.Notify(new UiNotificationMessage
    {
      Summary = summary,
      Detail = detail,
      Severity = UiNotificationSeverity.Warning
    });
  }

  public static void Info(this IUiNotificationService notificationService, string detail, string summary = "")
  {
    notificationService.Notify(new UiNotificationMessage
    {
      Summary = summary,
      Detail = detail,
      Severity = UiNotificationSeverity.Info
    });
  }
}
