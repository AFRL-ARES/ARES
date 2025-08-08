using System.Collections.Generic;
using Ares.Services;

namespace Ares.Core.Grpc.Services.Notifications;

public interface IAresNotificationRepo : ICollection<AresNotification>
{
}
