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
  private readonly IUiNotificationService _notificationService;

  public VisualizerSettingsListViewModel(
    VisualizerService visualizerService,
    IUiNotificationService notificationService)
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
      PushNotification(new UiNotificationMessage
      {
        Summary = "Error fetching visualizers",
        Detail = ex.Message,
        Severity = UiNotificationSeverity.Error
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
      PushNotification(new UiNotificationMessage
      {
        Summary = "Successfully Added Remote Visualizer",
        Detail = $"Added new visualizer {config.Name}",
        Severity = UiNotificationSeverity.Success
      });
      await UpdateAvailableVisualizers();
    }
    else
    {
      PushNotification(new UiNotificationMessage
      {
        Summary = $"Failed to Add Visualizer {config.Name}",
        Detail = response.ErrorMessage,
        Severity = UiNotificationSeverity.Error
      });
    }
  }

  private async Task OnVisualizerRemoved()
  {
    SettingsViewModels = null;
    await UpdateAvailableVisualizers();
  }

  public void PushNotification(UiNotificationMessage notification)
    => _notificationService.Notify(notification);

  [Reactive]
  public partial IEnumerable<VisualizerSettingsViewModel>? SettingsViewModels { get; private set; }

  [Reactive]
  public partial bool IsLoading { get; private set; }
}
