using Ares.Services;
using DynamicData;

namespace UI.Infrastructure.Notifications;

public class NotificationProvider : INotificationProvider
{
  private readonly INotificationRepo _notificationRepo;

  public NotificationProvider(INotificationRepo notificationRepo)
  {
    _notificationRepo = notificationRepo;
  }

  public int Count => _notificationRepo.Cache.Count;
  public IObservable<IChangeSet<AresNotification, string>> Connect() => _notificationRepo.Cache.Connect();
  public IReadOnlyCollection<AresNotification> GetAllNotifications() => _notificationRepo.GetNotifications();
  public AresNotification? GetNotification(string id) => _notificationRepo.GetNotification(id);
  public void MarkAsRead(string id) => _notificationRepo.MarkAsRead(id);
  public void MarkAllAsRead() => _notificationRepo.MarkAllAsRead();
  public bool HasUnread() => _notificationRepo.Cache.Items.Any(notif => !notif.IsRead);
}
