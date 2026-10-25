using Ares.Core.Grpc.Services;
using Ares.Datamodel.Connection;
using Ares.Datamodel.Visualizing;
using Ares.Services;
using ReactiveUI;
using UI.Application.Notifications;

namespace UI.Features.Visualizing.Settings;

public class VisualizerSettingsViewModel : ReactiveObject
{
  private readonly VisualizerService _visualizerService;
  private readonly IUiNotificationService _notificationService;
  private readonly VisualizerInfo _visualizerInfo;

  public VisualizerSettingsViewModel(
    VisualizerService visualizerService,
    IUiNotificationService notificationService,
    VisualizerInfo visualizerInfo,
    Func<Task> onRemoveCallback)
  {
    _visualizerService = visualizerService;
    _notificationService = notificationService;
    _visualizerInfo = visualizerInfo;
    Name = visualizerInfo.Name;
    Address = visualizerInfo.Url;
    Type = visualizerInfo.Type;
    Version = visualizerInfo.Version;
    Description = visualizerInfo.Description;
    EditViewModel = new VisualizerConfigEditViewModel(visualizerService, new VisualizerConfig
    {
      Name = visualizerInfo.Name,
      UniqueId = visualizerInfo.UniqueId,
      Url = visualizerInfo.Url
    });
    SettingsEditorViewModel = new VisualizerSettingsEditorViewModel(visualizerService, visualizerInfo);
    OnRemoveCallback = onRemoveCallback;
  }

  public string Name { get; private set; }
  public string Address { get; private set; } = "";
  public string Type { get; private set; } = "";
  public string Version { get; private set; } = "";
  public string Description { get; private set; } = "";
  public State VisualizerState { get; private set; }
  public string StateMessage { get; private set; } = "";
  public Func<Task> OnRemoveCallback { get; }
  public VisualizerConfigEditViewModel EditViewModel { get; }
  public VisualizerSettingsEditorViewModel SettingsEditorViewModel { get; }

  public void PushNotification(UiNotificationMessage notification)
    => _notificationService.Notify(notification);

  public async Task Save()
  {
    var config = EditViewModel.Save();
    var response = await _visualizerService.UpdateRemoteVisualizer(new UpdateRemoteVisualizerRequest
    {
      VisualizerId = _visualizerInfo.UniqueId,
      Name = config.Name,
      Url = config.Url
    }, null);

    if(response.Success)
    {
      Name = config.Name;
      Address = config.Url;
      PushNotification(new UiNotificationMessage
      {
        Summary = "Visualizer Update",
        Detail = $"Visualizer {Name} updated",
        Severity = UiNotificationSeverity.Success
      });
    }
    else
    {
      PushNotification(new UiNotificationMessage
      {
        Summary = "Visualizer Update",
        Detail = $"Visualizer {Name} failed to update.\n{response.ErrorMessage}",
        Severity = UiNotificationSeverity.Error
      });
    }

    await UpdateState();
  }

  public async Task SaveSettings()
    => await SettingsEditorViewModel.PushSettings();

  public async Task Remove()
  {
    await _visualizerService.RemoveRemoteVisualizer(new RemoveRemoteVisualizerRequest
    {
      VisualizerId = _visualizerInfo.UniqueId
    }, null);
    await OnRemoveCallback();
  }

  public async Task UpdateState()
  {
    try
    {
      var response = await _visualizerService.GetState(new StateRequest { Id = _visualizerInfo.UniqueId }, null);
      VisualizerState = response.State;
      StateMessage = response.StateMessage == string.Empty && response.State == State.Active
        ? "Visualizer is Active!"
        : response.StateMessage;
    }
    catch(Exception e)
    {
      VisualizerState = State.Error;
      StateMessage = $"Can't reach ares service: {e.Message}";
    }
  }

  public async Task UpdateInfo()
  {
    try
    {
      var response = await _visualizerService.GetInfo(new VisualizerInfoRequest { VisualizerId = _visualizerInfo.UniqueId }, null);
      Type = response.Info.Type;
      Name = response.Info.Name;
      Version = response.Info.Version;
      Description = response.Info.Description;
    }
    catch(Exception e)
    {
      Type = "";
      Name = "";
      Version = "";
      Description = $"Could not get info for the visualizer: {e.Message}";
    }
  }
}
