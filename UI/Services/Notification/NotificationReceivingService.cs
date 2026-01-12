using Ares.Services;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using NuGet.Packaging;
using Radzen;
using UI.Backend.Notifications;

namespace UI.Services.Notification;

public class NotificationReceivingService : INotificationReceivingService
{
  private readonly AresNotificationRpc.AresNotificationRpcClient _notificationClient;
  private readonly NotificationService _radzenNotificationService;
  private INotificationRepository _notificationRepo;
  public NotificationReceivingService(AresNotificationRpc.AresNotificationRpcClient notificationClient,
    NotificationService radzenNotificationService,
    INotificationRepository notificationRepo)
  {
    _notificationClient = notificationClient;
    _radzenNotificationService = radzenNotificationService;
    _notificationRepo = notificationRepo;
    _ = GetLatestNotificationHistory();
  }

  public void StartNotificationStream()
  {
    var subscriptionRequest = new SubscriptionRequest() { ClientId = Guid.NewGuid().ToString() };

    Task.Run(async () =>
    {
      Thread.CurrentThread.Name = "Notification Stream Thread";
      using(var stream = _notificationClient.Subscribe(subscriptionRequest))
      {
        try
        {
          await foreach(var notification in stream.ResponseStream.ReadAllAsync())
          {
            var userNotification = new NotificationMessage();
            userNotification.Summary = notification.Title;
            userNotification.Detail = notification.Message;
            userNotification.Severity = ConvertToRadzenSeverity(notification.NotificationSeverity);
            userNotification.Duration = DetermineDisplayTime(notification.NotificationSeverity, notification.Loiter);
            userNotification.CloseOnClick = notification.NotificationSeverity == Severity.Danger;

            _radzenNotificationService.Notify(userNotification);
            _notificationRepo.Add(notification);
          }
        }

        catch(RpcException ex)
        {
          Console.WriteLine($"Error receiving notifications: {ex.Status}");
        }
      }
    });

  }

  public void PushNotification(AresNotification notification)
  {
    notification.Title = notification.Title ?? string.Empty;
    notification.Message = notification.Message ?? string.Empty;

    var radzenNotification = new NotificationMessage();
    radzenNotification.Summary = notification.Title;
    radzenNotification.Detail = notification.Message;
    radzenNotification.Severity = ConvertToRadzenSeverity(notification.NotificationSeverity);
    radzenNotification.Duration = DetermineDisplayTime(notification.NotificationSeverity, notification.Loiter);
    radzenNotification.CloseOnClick = true;

    if(notification.Timestamp is null)
      notification.Timestamp = DateTime.UtcNow.ToTimestamp();

    _radzenNotificationService.Notify(radzenNotification);
    _notificationRepo.Add(notification);
  }

  public async Task GetLatestNotificationHistory()
  {
    var history = await _notificationClient.GetUpdatedNotificationListAsync(new Empty());
    _notificationRepo.AddRange(history.Notifications);
  }

  private bool CloseOnClick(Severity severity, bool loiter) => severity == Severity.Danger || loiter;

  private Int32 DetermineDisplayTime(Severity severity, bool loiter)
  {
    if(loiter)
      return Int32.MaxValue;

    switch(severity)
    {
      case Severity.Unspecified:
        return 5000;
      case Severity.Info:
        return 3000;
      case Severity.Warning:
        return 5000;
      case Severity.Error:
        return 8000;
      case Severity.Danger:
        return 100000;
      case Severity.Success:
        return 5000;
      default:
        return 5000;
    }
  }

  private NotificationSeverity ConvertToRadzenSeverity(Severity aresSeverity)
  {
    switch(aresSeverity)
    {
      case Severity.Error:
        return NotificationSeverity.Error;

      case Severity.Success:
        return NotificationSeverity.Success;

      case Severity.Info:
        return NotificationSeverity.Info;

      case Severity.Warning:
        return NotificationSeverity.Warning;

      case Severity.Danger:
        return NotificationSeverity.Error;

      case Severity.Unspecified:
        return NotificationSeverity.Info;

      default:
        return NotificationSeverity.Info;
    }
  }
}
