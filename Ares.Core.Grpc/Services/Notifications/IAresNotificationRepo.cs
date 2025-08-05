using Ares.Messaging;
using System.Collections.Generic;

namespace Ares.Core.Grpc.Services.Notifications;

public interface IAresNotificationRepo : ICollection<AresNotification>
{
}
