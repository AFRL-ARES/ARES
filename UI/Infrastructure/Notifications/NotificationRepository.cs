using Ares.Services;
using UI.Application.Notifications;

namespace UI.Infrastructure.Notifications;

public class NotificationRepository : List<AresNotification>, INotificationRepository
{
}

