using Ares.Services;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using NuGet.Packaging;
using Radzen;
using UI.Backend.Notifications;

namespace UI.Services.Notification;

public class NotificationReceivingService : INotificationReceivingService, IDisposable
{
  private readonly AresNotificationRpc.AresNotificationRpcClient _notificationClient;
  private readonly NotificationService _radzenNotificationService;
  private readonly INotificationRepository _notificationRepo;
  private readonly CancellationTokenSource _cts = new();
  private readonly ILogger<NotificationReceivingService> _logger;

  public NotificationReceivingService(
      AresNotificationRpc.AresNotificationRpcClient notificationClient,
      NotificationService radzenNotificationService,
      INotificationRepository notificationRepo,
      ILogger<NotificationReceivingService> logger)
  {
    _notificationClient = notificationClient;
    _radzenNotificationService = radzenNotificationService;
    _notificationRepo = notificationRepo;
    _logger = logger;
  }

  public async Task InitializeAsync()
  {
    try
    {
      var history = await _notificationClient.GetUpdatedNotificationListAsync(new Empty());
      _notificationRepo.AddRange(history.Notifications);

      StartNotificationStream();
    }
    catch(RpcException ex)
    {
      _logger.LogError($"Failed to fetch parameter history: {ex.Message}");
      StartNotificationStream();
    }
  }

  public void StartNotificationStream()
  {
    _ = Task.Run(async () => await RunStreamLoop(_cts.Token));
  }

  private async Task RunStreamLoop(CancellationToken token)
  {
    var subscriptionRequest = new SubscriptionRequest() { ClientId = Guid.NewGuid().ToString() };

    while(!token.IsCancellationRequested)
    {
      try
      {
        using var stream = _notificationClient.Subscribe(subscriptionRequest, cancellationToken: token);

        await foreach(var notification in stream.ResponseStream.ReadAllAsync(token))
        {
          HandleIncomingNotification(notification);
        }
      }
      catch(RpcException ex) when(ex.StatusCode == StatusCode.Cancelled)
      {
        break;
      }
      catch(RpcException ex)
      {
        Console.WriteLine($"Notification stream error: {ex.Status}. Retrying in 5 seconds...");
        _logger.LogError($"RPC Exception occured in notification stream: {ex.Message} ");
        _logger.LogError(ex.StackTrace);
        await Task.Delay(5000, token);
      }
      catch(Exception ex)
      {
        Console.WriteLine($"Critical Stream Error: {ex.Message}");
        _logger.LogError($"Exception occured in notification stream: {ex.Message} ");
        _logger.LogError(ex.StackTrace);
        await Task.Delay(5000, token);
      }
    }
  }

  public void PushNotification(AresNotification notification)
  {
    notification.Timestamp ??= DateTime.UtcNow.ToTimestamp();
    notification.Title ??= string.Empty;
    notification.Message ??= string.Empty;

    _radzenNotificationService.Notify(notification.ToRadzenMessage());
    _notificationRepo.Add(notification);
  }

  private void HandleIncomingNotification(AresNotification notification)
  {
    PushNotification(notification);
  }

  public void Dispose()
  {
    _cts.Cancel();
    _cts.Dispose();
  }
}