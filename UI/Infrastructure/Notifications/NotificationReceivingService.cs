using Ares.Services;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using NuGet.Packaging;
using UI.Domain.Notifications;

namespace UI.Infrastructure.Notifications;

public class NotificationReceivingService : INotificationReceivingService
{
  private readonly AresNotificationRpc.AresNotificationRpcClient _notificationClient;
  private readonly IUiNotificationService _uiNotificationService;
  private readonly INotificationRepository _notificationRepo;

  public NotificationReceivingService(
    AresNotificationRpc.AresNotificationRpcClient notificationClient,
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
      using var stream = _notificationClient.Subscribe(subscriptionRequest);
      try
      {
        await foreach (var notification in stream.ResponseStream.ReadAllAsync())
        {
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
      catch(RpcException ex)
      {
        Console.WriteLine($"Error receiving notifications: {ex.Status}");
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
    var history = await _notificationClient.GetUpdatedNotificationListAsync(new Empty());
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
