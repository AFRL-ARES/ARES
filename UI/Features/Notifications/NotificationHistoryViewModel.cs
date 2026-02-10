using Ares.Services;
using ReactiveUI;
using UI.Domain.Notifications;

namespace UI.Features.Notifications;

public class NotificationHistoryViewModel : ReactiveObject
{
  public NotificationHistoryViewModel(INotificationRepository notificationRepo)
  {
    NotificationRepo = notificationRepo;
  }

  public string ConvertSeverityToCssClass(Severity severity)
  {
    switch(severity)
    {
      case Severity.Unspecified:
        return "rz-background-color-info-lighter rz-color-on-info-lighter ares-notification";
      case Severity.Info:
        return "rz-background-color-info-lighter rz-color-on-info-lighter ares-notification";
      case Severity.Warning:
        return "rz-background-color-warning-lighter rz-color-on-warning-lighter ares-notification";
      case Severity.Error:
        return "ares-notification rz-background-color-danger-lighter rz-color-on-danger-lighter";
      case Severity.Danger:
        return "rz-background-color-danger-lighter rz-color-on-danger-lighter ares-notification";
      case Severity.Success:
        return "rz-background-color-success-lighter rz-color-on-success-lighter ares-notification";
      default:
        return "rz-background-color-info-lighter rz-color-on-info-lighter ares-notification";
    }
  }

  public bool ShouldDisplayNotification(AresNotification notification)
  {
    if(DisplayAllNotifications)
      return true;

    switch(notification.NotificationSeverity)
    {
      case Severity.Unspecified:
        return true;
      case Severity.Info:
        return DisplayInfoNotifications;
      case Severity.Warning:
        return DisplayWarningNotifications;
      case Severity.Error:
        return DisplayErrorNotifications;
      case Severity.Danger:
        return true;
      case Severity.Success:
        return DisplaySuccessNotifications;
      default:
        return true;
    }
  }

  public INotificationRepository NotificationRepo { get; set; }
  public bool DisplayAllNotifications { get; set; } = true;
  public bool DisplayErrorNotifications { get; set; } = true;
  public bool DisplayWarningNotifications { get; set; } = true;
  public bool DisplayInfoNotifications { get; set; } = true;
  public bool DisplaySuccessNotifications { get; set; } = true;
  public int NotificationSortMethod { get; set; } = 0;
}

