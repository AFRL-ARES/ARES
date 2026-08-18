using Ares.Services;
using DynamicData;

namespace UI.Infrastructure.Notifications;

public class NotificationRepo : INotificationRepo
{
  private readonly SourceCache<AresNotification, string> _notificationCache = new(c => c.UniqueId);
  public ISourceCache<AresNotification, string> Cache => _notificationCache;

  public AresNotification? GetNotification(string id)
  {
    var lookup = _notificationCache.Lookup(id);
    return lookup.HasValue ? lookup.Value : null;
  }

  public void AddOrUpdate(AresNotification notification) => _notificationCache.AddOrUpdate(notification);

  public void AddRange(IEnumerable<AresNotification> notifications)
  {
    foreach(var notification in notifications)
      _notificationCache.AddOrUpdate(notification);
  }

  public void MarkAsRead(string id)
  {
    var lookup = _notificationCache.Lookup(id);

    if(lookup.HasValue)
    {
      lookup.Value.IsRead = true;
      _notificationCache.AddOrUpdate(lookup.Value);
    }
  }

  public void MarkAllAsRead()
  {
    foreach(var notif in _notificationCache.Items)
    {
      notif.IsRead = true;
      _notificationCache.AddOrUpdate(notif);
    }
  }

  public IEnumerator<AresNotification> GetEnumerator() => _notificationCache.Items.GetEnumerator();
  public IReadOnlyCollection<AresNotification> GetNotifications() => _notificationCache.Items.ToList().AsReadOnly();
  public void Remove(string id) => _notificationCache.Remove(id);
  public void Clear() => _notificationCache.Clear();

  public void Dispose()
  {
    _notificationCache.Dispose();
    GC.SuppressFinalize(this);
  }
}
