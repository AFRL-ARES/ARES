using Ares.Messaging;
using Ares.Messaging.Analyzing;
using Grpc.Core;
using ReactiveUI;
using UI.Services.Notification;

namespace UI.Backend.ViewModels.Settings.Analysis;

public class AnalyzerSettingsViewModel : ReactiveObject
{
  private readonly AresAnalyzerManagementService.AresAnalyzerManagementServiceClient _analyzerService;
  private readonly INotificationReceivingService _notificationService;
  private AnalyzerInfo _analyzerInfo;

  public AnalyzerSettingsViewModel(AresAnalyzerManagementService.AresAnalyzerManagementServiceClient analyzerService,
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
    OnRemoveCallback = onRemoveCallback;
    _notificationService = notificationService;
  }

  public void PushNotification(AresNotification notification) => _notificationService.PushNotification(notification);

  public string Name { get; private set; }

  public string Address { get; private set; } = "";

  public string Type { get; private set; } = "";

  public string Version { get; private set; } = "";

  public string Description { get; set; } = "";

  public AnalyzerState AnalyzerState { get; private set; }

  public string StateMessage { get; private set; } = "";

  public Func<Task> OnRemoveCallback { get; }

  public AnalyzerConfigEditViewModel EditViewModel { get; }

  public async Task Save()
  {
    var analyzerConfig = EditViewModel.Save();
    var request = new UpdateRemoteAnalyzerRequest
    {
      AnalyzerId = _analyzerInfo.UniqueId,
      Name = analyzerConfig.Name,
      Url = analyzerConfig.Url
    };
    var response = await _analyzerService.UpdateRemoteAnalyzerAsync(request);
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

    await _analyzerService.RemoveRemoteAnalyzerAsync(request);
    await OnRemoveCallback();
  }

  public async Task UpdateState()
  {
    var request = new AnalyzerStateRequest { AnalyzerId = _analyzerInfo.UniqueId };
    try
    {
      var stateResponse = await _analyzerService.GetStateAsync(request);
      StateMessage = stateResponse.StateMessage;
      AnalyzerState = stateResponse.State;
    }
    catch(RpcException e)
    {
      StateMessage = $"Can't reach ares service: {e.Message}";
      AnalyzerState = AnalyzerState.Error;
    }
  }

  public AresDataSchema SettingsSchema { get; private set; } = new AresDataSchema();

  public AresStruct Settings { get; private set; } = new AresStruct();

  public async Task FetchSettings()
  {
    var request = new AnalyzerSettingsRequest() { AnalyzerId = _analyzerInfo.UniqueId };
    try
    {
      var settingsResponse = await _analyzerService.GetAnalyzerSettingsAsync(request);
      Settings = settingsResponse;
    }
    catch(RpcException)
    {
      Settings = new AresStruct();
    }
  }

  public async Task PushSettings()
  {
    var settings = new AnalyzerSettings
    {
      AnalyzerId = _analyzerInfo.UniqueId,
      Settings = Settings
    };
    try
    {
      await _analyzerService.SetAnalyzerSettingsAsync(settings);
    }
    catch (RpcException)
    {
      // TODO maybe notify user
    }
  }

  public async Task UpdateInfo()
  {
    var request = new AnalyzerInfoRequest { AnalyzerId = _analyzerInfo.UniqueId };
    try
    {
      var infoResponse = await _analyzerService.GetInfoAsync(request);
      Type = infoResponse.Info.Type;
      Name = infoResponse.Info.Name;
      Version = infoResponse.Info.Version;
      Description = infoResponse.Info.Description;
      SettingsSchema = infoResponse.Info.Capabilities?.SettingsSchema ?? new AresDataSchema();
    }
    catch(RpcException e)
    {
      Type = "";
      Name = "";
      Version = "";
      Description = $"Could not get info for the analyzer: {e.Message}";
    }
  }
}
