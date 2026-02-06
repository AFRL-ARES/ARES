namespace UI.Services.Notification;

public sealed record UiNotificationMessage
{
  public string Summary { get; set; } = string.Empty;
  public string Detail { get; set; } = string.Empty;
  public UiNotificationSeverity Severity { get; set; } = UiNotificationSeverity.Info;
  public int DurationMs { get; set; } = 5000;
  public bool CloseOnClick { get; set; } = true;
}
