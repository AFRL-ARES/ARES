namespace UI.Services.Notification;

internal record AresNotification(string Title, string Message)
{
  public NotificationSeverity Severity { get; init; }
  public bool Acknowledged { get; set; }
}
