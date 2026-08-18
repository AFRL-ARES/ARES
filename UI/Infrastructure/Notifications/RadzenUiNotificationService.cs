using Ares.Services;
using Google.Protobuf.WellKnownTypes;
using Radzen;
using UI.Application.Notifications;

namespace UI.Infrastructure.Notifications;

internal sealed class RadzenUiNotificationService : IUiNotificationService
{
  private readonly NotificationService _notificationService;
  private readonly INotificationRepo _notificationRepo;

  public RadzenUiNotificationService(NotificationService notificationService, INotificationRepo notificationRepo)
  {
    _notificationService = notificationService;
    _notificationRepo = notificationRepo;
  }

  public void Notify(UiNotificationMessage message)
  {
    var radzenNotification = new NotificationMessage
    {
      Summary = message.Summary,
      Detail = message.Detail,
      Severity = ConvertToRadzenSeverity(message.Severity),
      Duration = message.DurationMs,
      CloseOnClick = message.CloseOnClick
    };

    _notificationService.Notify(radzenNotification);

    //Add to the repo for tracking in history
    var aresNotification = new AresNotification
    {
      Title = message.Summary,
      Message = message.Detail,
      NotificationSeverity = ConvertToAresSeverity(message.Severity),
      Loiter = message.CloseOnClick,
      Timestamp = Timestamp.FromDateTime(DateTime.UtcNow),
      UniqueId = Guid.NewGuid().ToString()
    };

    _notificationRepo.AddOrUpdate(aresNotification);
  }

  private static NotificationSeverity ConvertToRadzenSeverity(UiNotificationSeverity severity)
  {
    return severity switch
    {
      UiNotificationSeverity.Error => NotificationSeverity.Error,
      UiNotificationSeverity.Success => NotificationSeverity.Success,
      UiNotificationSeverity.Warning => NotificationSeverity.Warning,
      _ => NotificationSeverity.Info
    };
  }

  private static Severity ConvertToAresSeverity(UiNotificationSeverity severity)
  {
    return severity switch
    { 
      UiNotificationSeverity.Error => Severity.Error,
      UiNotificationSeverity.Success => Severity.Success,
      UiNotificationSeverity.Warning => Severity.Warning,
      _ => Severity.Info
    };
  }
}

