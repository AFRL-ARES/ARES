using Ares.Services;
using DynamicData;

namespace UI.Infrastructure.Notifications;

/// <summary>
/// This notification repo serves as a storage point for the UI to access notifications locally as they are supplied by the ARES Core
/// </summary>
public interface INotificationRepo : IDisposable
{
  /// <summary>
  /// Storage engine accessors use by Managers and Providers
  /// </summary>
  ISourceCache<AresNotification, string> Cache { get; }

  /// <summary>
  /// Gets a notification from storage by it's corresponding unique id.
  /// </summary>
  /// <param name="id"></param>
  /// <returns></returns>
  AresNotification? GetNotification(string id);

  /// <summary>
  /// Retreieves all currently available notifications.
  /// </summary>
  /// <returns>A read-only collection of (<see cref="AresNotification"/></returns>
  IReadOnlyCollection<AresNotification> GetNotifications();

  /// <summary>
  /// Marks a notification as read in the repo
  /// </summary>
  /// <param name="id"></param>
  void MarkAsRead(string id);

  /// <summary>
  /// Marks all existing notifications as read
  /// </summary>
  void MarkAllAsRead();

  /// <summary>
  /// Adds a notification to the repo or updates it if it already exists
  /// </summary>
  /// <param name="notification"></param>
  void AddOrUpdate(AresNotification notification);

  /// <summary>
  /// Adds a list of notifications to the repo
  /// </summary>
  /// <param name="notification"></param>
  void AddRange(IEnumerable<AresNotification> notification);

  /// <summary>
  /// Removes a notification matching the provided unique id
  /// </summary>
  /// <param name="id"></param>
  void Remove(string id);

  /// <summary>
  /// Purges all notifications from the storage
  /// </summary>
  void Clear();
}
