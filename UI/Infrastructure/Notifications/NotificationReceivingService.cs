using Ares.Services;
using Ares.Core.Grpc.Services.Notifications;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using NuGet.Packaging;
using UI.Application.Notifications;
using UI.Infrastructure.Grpc;

namespace UI.Infrastructure.Notifications;

public class NotificationReceivingService : INotificationReceivingService
{
  private readonly AresNotificationService _notificationClient;
  private readonly IUiNotificationService _uiNotificationService;
  private readonly INotificationRepository _notificationRepo;

  public NotificationReceivingService(
    AresNotificationService notificationClient,
    IUiNotificationService uiNotificationService,
    INotificationRepository notificationRepo)
  {
    _notificationClient = notificationClient;
    _uiNotificationService = uiNotificationService;
    _notificationRepo = notificationRepo;
    _ = GetLatestNotificationHistory();
  }

  public void StartNotificationStream()
  {
    var subscriptionRequest = new SubscriptionRequest { ClientId = Guid.NewGuid().ToString() };

    Task.Run(async () =>
    {
      var stream = new LocalStream<AresNotification>();
      _ = _notificationClient.Subscribe(subscriptionRequest, stream, null);
      try
      {
        while (await stream.MoveNext(default))
        {
          var notification = stream.Current;
          var userNotification = new UiNotificationMessage
          {
            Summary = notification.Title,
            Detail = notification.Message,
            Severity = ConvertToUiSeverity(notification.NotificationSeverity),
            DurationMs = DetermineDisplayTime(notification.NotificationSeverity, notification.Loiter),
            CloseOnClick = notification.NotificationSeverity == Severity.Danger
          };

          _uiNotificationService.Notify(userNotification);
          _notificationRepo.Add(notification);
        }
      }
      catch(Exception ex)
      {
        Console.WriteLine($"Error receiving notifications: {ex.Message}");
      }
    });
  }

  public void PushNotification(AresNotification notification)
  {
    notification.Title ??= string.Empty;
    notification.Message ??= string.Empty;

    var uiNotification = new UiNotificationMessage
    {
      Summary = notification.Title,
      Detail = notification.Message,
      Severity = ConvertToUiSeverity(notification.NotificationSeverity),
      DurationMs = DetermineDisplayTime(notification.NotificationSeverity, notification.Loiter),
      CloseOnClick = true
    };

    if(notification.Timestamp is null)
      notification.Timestamp = DateTime.UtcNow.ToTimestamp();

    _uiNotificationService.Notify(uiNotification);
    _notificationRepo.Add(notification);
  }

  public async Task GetLatestNotificationHistory()
  {
    var history = await _notificationClient.GetUpdatedNotificationList(new Empty(), null);
    _notificationRepo.AddRange(history.Notifications);
  }

  private static int DetermineDisplayTime(Severity severity, bool loiter)
  {
    if(loiter)
      return int.MaxValue;

    return severity switch
    {
      Severity.Info => 3000,
      Severity.Warning => 5000,
      Severity.Error => 8000,
      Severity.Danger => 100000,
      Severity.Unspecified => 5000,
      Severity.Success => 5000,

      _ => 5000
    };
  }

  private static UiNotificationSeverity ConvertToUiSeverity(Severity aresSeverity)
  {
    return aresSeverity switch
    {
      Severity.Error => UiNotificationSeverity.Error,
      Severity.Success => UiNotificationSeverity.Success,
      Severity.Warning => UiNotificationSeverity.Warning,
      Severity.Danger => UiNotificationSeverity.Error,
      _ => UiNotificationSeverity.Info
    };
  }
}

