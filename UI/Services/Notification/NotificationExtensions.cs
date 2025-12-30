using Ares.Services;
using Radzen;

public static class NotificationExtensions
{
  public static NotificationMessage ToRadzenMessage(this AresNotification n)
  {
    var severity = n.NotificationSeverity.ToRadzenSeverity();

    return new NotificationMessage
    {
      Summary = n.Title,
      Detail = n.Message,
      Severity = severity,
      Duration = GetDuration(n.NotificationSeverity, n.Loiter),
      CloseOnClick = severity == NotificationSeverity.Error || n.Loiter // Simplified logic
    };
  }

  public static NotificationSeverity ToRadzenSeverity(this Severity s) => s switch
  {
    Severity.Success => NotificationSeverity.Success,
    Severity.Info => NotificationSeverity.Info,
    Severity.Warning => NotificationSeverity.Warning,
    Severity.Error => NotificationSeverity.Error,
    Severity.Danger => NotificationSeverity.Error,
    _ => NotificationSeverity.Info
  };

  private static int GetDuration(Severity s, bool loiter)
  {
    if(loiter)
      return int.MaxValue;

    return s switch
    {
      Severity.Info => 3000,
      Severity.Error => 8000,
      Severity.Danger => 100000,
      _ => 5000
    };
  }
}