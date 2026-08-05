using Ares.Services;
using Ares.Core.Grpc.Services.Notifications;
using Google.Protobuf.WellKnownTypes;
using UI.Application.Notifications;
using UI.Infrastructure.Grpc;
using Radzen;

namespace UI.Infrastructure.Notifications;

public class NotificationReceivingService : INotificationReceivingService
{
  private readonly AresNotificationService _notificationClient;
  private readonly INotificationRepo _notificationRepo;
  private readonly NotificationService _radzenNotificationService;
  public event Action<UiNotificationMessage>? OnNotificationReceived;

  public NotificationReceivingService(AresNotificationService notificationClient, INotificationRepo notificationRepo, NotificationService radzenNotificationService)
  {
    _notificationClient = notificationClient;
    _notificationRepo = notificationRepo;
    _radzenNotificationService = radzenNotificationService;
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
            CloseOnClick = notification.NotificationSeverity == Severity.Danger,
          };

          var radzenNotification = new NotificationMessage
          {
            Summary = notification.Title,
            Detail = notification.Message,
            Severity = ConvertToRadzenSeverity(notification.NotificationSeverity),
            CloseOnClick = notification.NotificationSeverity == Severity.Danger,
            Duration = DetermineDisplayTime(notification.NotificationSeverity, notification.Loiter)
          };

          //Add to the repo
          _notificationRepo.AddOrUpdate(notification);
          //Pass to Radzen (for popup toast)
          _radzenNotificationService.Notify(radzenNotification);
          OnNotificationReceived?.Invoke(userNotification);
        }
      }
      catch(Exception ex)
      {
        Console.WriteLine($"Error receiving notifications: {ex.Message}");
      }
    });
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

  private static NotificationSeverity ConvertToRadzenSeverity(Severity severity)
  {
    return severity switch
    {
      Severity.Error => NotificationSeverity.Error,
      Severity.Success => NotificationSeverity.Success,
      Severity.Warning => NotificationSeverity.Warning,
      _ => NotificationSeverity.Info
    };
  }
}

