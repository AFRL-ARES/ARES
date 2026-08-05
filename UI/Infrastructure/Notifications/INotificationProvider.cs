using Ares.Services;
using DynamicData;

namespace UI.Infrastructure.Notifications;

/// <summary>
/// The notification provider is designed to give UI components controlled access to existing notifications, with limited edit abilities such as marking them as read
/// </summary>
public interface INotificationProvider
{
  /// <summary>
  /// Provides a reactive stream of changes. 
  /// Subscribers automatically receive the current state followed by updates.
  /// </summary>
  IObservable<IChangeSet<AresNotification, string>> Connect();

  /// <summary>
  /// Retrieves a device config by its unique ID
  /// </summary>
  AresNotification? GetNotification(string id);

  /// <summary>
  /// Marks a notification as read by it's ID
  /// </summary>
  /// <param name="id"></param>
  void MarkAsRead(string id);

  /// <summary>
  /// Marks all existing notifications in the repositor as read
  /// </summary>
  void MarkAllAsRead();

  /// <summary>
  /// Checks whether there are any unread notifications present in the repository
  /// </summary>
  /// <returns>A boolean value indicating whether there are any unread notifications in the repo</returns>
  bool HasUnread();

  /// <summary>
  /// Retrieves a read-only snapshot of all currently available notifications.
  /// </summary>
  IReadOnlyCollection<AresNotification> GetAllNotifications();

  /// <summary>
  /// The total number of notifications currently in the repository.
  /// </summary>
  int Count { get; }
}
