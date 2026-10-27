using Ares.Core.Grpc.Services;
using Ares.Datamodel.Visualizing;
using Ares.Services;
using Google.Protobuf.WellKnownTypes;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using UI.Application.Notifications;

namespace UI.Features.Visualizing.Settings;

public partial class VisualizerSettingsListViewModel : ReactiveObject
{
  private readonly VisualizerService _visualizerService;
  private readonly IAresUINotificationService _notificationService;

  public VisualizerSettingsListViewModel(
    VisualizerService visualizerService,
    IAresUINotificationService notificationService)
  {
    _visualizerService = visualizerService;
    _notificationService = notificationService;
  }

  public VisualizerConfigEditViewModel GetNewConfigEditViewModel()
    => new(_visualizerService);

  public async Task UpdateAvailableVisualizers()
  {
    IsLoading = true;
    try
    {
      var response = await _visualizerService.GetAllVisualizers(new Empty(), null);
      var visualizers = response.Visualizers.Where(info => !info.Name.Equals("NONE"));
      SettingsViewModels = visualizers
        .Select(info => new VisualizerSettingsViewModel(_visualizerService, _notificationService, info, OnVisualizerRemoved))
        .ToArray();
    }
    catch(Exception ex)
    {
      PushNotification(new AresNotification
      {
        Title = "Error fetching visualizers",
        Message = ex.Message,
        NotificationSeverity = Severity.Error
      });
    }
    finally
    {
      IsLoading = false;
    }
  }

  public async Task AddNewVisualizer(VisualizerConfig config)
  {
    var response = await _visualizerService.AddRemoteVisualizer(new AddRemoteVisualizerRequest
    {
      Name = config.Name,
      Url = config.Url
    }, null);

    if(response.Success)
    {
      PushNotification(new AresNotification
      {
        Title = "Successfully Added Remote Visualizer",
        Message = $"Added new visualizer {config.Name}",
        NotificationSeverity = Severity.Success
      });
      await UpdateAvailableVisualizers();
    }
    else
    {
      PushNotification(new AresNotification
      {
        Title = $"Failed to Add Visualizer {config.Name}",
        Message = response.ErrorMessage,
        NotificationSeverity = Severity.Error
      });
    }
  }

  private async Task OnVisualizerRemoved()
  {
    SettingsViewModels = null;
    await UpdateAvailableVisualizers();
  }

  public void PushNotification(AresNotification notification)
    => _notificationService.PushNotification(notification);

  [Reactive]
  public partial IEnumerable<VisualizerSettingsViewModel>? SettingsViewModels { get; private set; }

  [Reactive]
  public partial bool IsLoading { get; private set; }
}
