using Ares.Datamodel.Analyzing;
using Ares.Datamodel.Connection;
using Ares.Services;
using Ares.Core.Grpc.Services;
using Grpc.Core;
using ReactiveUI;
using UI.Application.Notifications;

namespace UI.Features.Analyzing.Settings;

public class AnalyzerSettingsViewModel : ReactiveObject
{
  private readonly AnalyzerService _analyzerService;
  private readonly INotificationReceivingService _notificationService;
  private AnalyzerInfo _analyzerInfo;

  public AnalyzerSettingsViewModel(AnalyzerService analyzerService,
    INotificationReceivingService notificationService,
    AnalyzerInfo analyzerInfo,
    Func<Task> onRemoveCallback)
  {
    _analyzerService = analyzerService;
    _analyzerInfo = analyzerInfo;
    Name = _analyzerInfo.Name;
    Address = _analyzerInfo.Url;
    Type = _analyzerInfo.Type;
    EditViewModel = new AnalyzerConfigEditViewModel(analyzerService, new AnalyzerConfig() { Name = analyzerInfo.Name, UniqueId = analyzerInfo.UniqueId, Url = analyzerInfo.Url });
    SettingsEditorViewModel = new AnalyzerSettingsEditorViewModel(analyzerService, analyzerInfo);
    OnRemoveCallback = onRemoveCallback;
    _notificationService = notificationService;
  }

  public void PushNotification(AresNotification notification) => _notificationService.PushNotification(notification);

  public string Name { get; private set; }

  public string Address { get; private set; } = "";

  public string Type { get; private set; } = "";

  public string Version { get; private set; } = "";

  public string Description { get; set; } = "";

  public State AnalyzerState { get; private set; }

  public string StateMessage { get; private set; } = "";

  public Func<Task> OnRemoveCallback { get; }

  public AnalyzerConfigEditViewModel EditViewModel { get; }

  public AnalyzerSettingsEditorViewModel SettingsEditorViewModel { get; }

  public async Task Save()
  {
    var analyzerConfig = EditViewModel.Save();
    var request = new UpdateRemoteAnalyzerRequest
    {
      AnalyzerId = _analyzerInfo.UniqueId,
      Name = analyzerConfig.Name,
      Url = analyzerConfig.Url
    };
    var response = await _analyzerService.UpdateRemoteAnalyzer(request, null);
    if(response.Success)
    {
      Name = analyzerConfig.Name;
      Address = analyzerConfig.Url;
      PushNotification(
        new AresNotification
        {
          Title = "Analyzer Update",
          Message = $"Analyzer {Name} updated",
          NotificationSeverity = Severity.Success
        });
    }
    else
    {
      PushNotification(
        new AresNotification
        {
          Title = "Analyzer Update",
          Message = $"Analyzer {Name} failed to update.\n{response.ErrorMessage}",
          NotificationSeverity = Severity.Error
        });
    }
    await UpdateState();
  }

  public async Task Remove()
  {
    var request = new RemoveRemoteAnalyzerRequest
    {
      AnalyzerId = _analyzerInfo.UniqueId
    };

    await _analyzerService.RemoveRemoteAnalyzer(request, null);
    await OnRemoveCallback();
  }

  public async Task UpdateState()
  {
    var request = new StateRequest { Id = _analyzerInfo.UniqueId };
    try
    {
      var stateResponse = await _analyzerService.GetState(request, null);
      StateMessage = stateResponse.StateMessage;
      AnalyzerState = stateResponse.State;
    }
    catch(Exception e)
    {
      StateMessage = $"Can't reach ares service: {e.Message}";
      AnalyzerState = State.Error;
    }
  }

  public async Task UpdateInfo()
  {
    var request = new AnalyzerInfoRequest { AnalyzerId = _analyzerInfo.UniqueId };
    try
    {
      var infoResponse = await _analyzerService.GetInfo(request, null);
      Type = infoResponse.Info.Type;
      Name = infoResponse.Info.Name;
      Version = infoResponse.Info.Version;
      Description = infoResponse.Info.Description;
    }
    catch(Exception e)
    {
      Type = "";
      Name = "";
      Version = "";
      Description = $"Could not get info for the analyzer: {e.Message}";
    }
  }
}


