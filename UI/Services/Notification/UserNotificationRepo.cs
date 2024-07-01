using DynamicData;

namespace UI.Services.Notification;

internal class UserNotificationRepo
{
  public ISourceList<AresNotification> Repo { get; } = new SourceList<AresNotification>();
}
