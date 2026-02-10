using Ares.Services;
using UI.Domain.Notifications;

namespace UI.Infrastructure.Notifications;

public class NotificationRepository : List<AresNotification>, INotificationRepository
{
}
