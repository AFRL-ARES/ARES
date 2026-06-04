using Ares.Core.Notifications;
using Ares.Core.Settings;
using Ares.Datamodel;
using Google.Protobuf.WellKnownTypes;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace UI.Features.Settings;

public partial class SystemSettingsViewModel : ReactiveObject
{
  private readonly ISystemSettingsManager _settingsManager;
  private readonly INotificationHandler _notificationHandler;
  private readonly ILogger<SystemSettingsViewModel> _logger;

  public SystemSettingsViewModel(ISystemSettingsManager settingsManager, INotificationHandler notificationHandler, ILogger<SystemSettingsViewModel> logger)
  {
    _settingsManager = settingsManager;
    _notificationHandler = notificationHandler;
    CurrentErrorHandlingSettings = new();
    Initialize();
    _logger = logger;
  }

  public async Task PushUpdatedSettings()
  {
    try
    {
      var configs = CurrentErrorHandlingSettings.Select(kvp => new DeviceErrorHandlingConfig()
      {
        Code = kvp.Key,
        Handling = kvp.Value
      }).ToList();

      await _settingsManager.UpdateErrorHandlingSettings(configs);
      await _settingsManager.UpdateAresGeneralSettings(new AresGeneralSettingsConfig
      {
        ExperimentRetryLimit = ExperimentRetryLimit,
        RetryCooldown = new Duration() { Seconds = ExperimentRetryCooldown },
        CommandLatency = new Duration() { Seconds = CommandLatency },
        CommandRetryLimit = CommandRetryLimit
      });

      await _notificationHandler.HandleNotification("Settings Updated!", "ARES successfully updated your settings.", NotificationSeverityEnum.Success);
      _logger.LogInformation("ARES settings successfully updated");
    }

    catch(Exception e)
    {
      await _notificationHandler.HandleNotification("Failed to Update Settings", $"ARES couldn't update your settings. {e.Message}", NotificationSeverityEnum.Success);
      _logger.LogError("ARES failed to update settings. Error: {error}", e.Message);
    }
  }

  public async Task GetUpdatedSettings()
  {
    var newErrorHandlingSettings = await _settingsManager.GetCurrentErrorHandlingSettings();
    var newGeneralSettings = await _settingsManager.GetAresGeneralSettings();

    foreach(var setting in newErrorHandlingSettings)
      CurrentErrorHandlingSettings[setting.Code] = setting.Handling;

    if(newGeneralSettings is not null)
    {
      ExperimentRetryCooldown = (int)newGeneralSettings.RetryCooldown.Seconds; 
      ExperimentRetryLimit = newGeneralSettings.ExperimentRetryLimit;
      CommandLatency = (int)newGeneralSettings.CommandLatency.Seconds;
      CommandRetryLimit = newGeneralSettings.CommandRetryLimit;
    }
  }

  private void Initialize()
  {
    foreach(var status in System.Enum.GetValues<CommandStatusCode>())
    {
      if(!CurrentErrorHandlingSettings.TryGetValue(status, out var val))
        CurrentErrorHandlingSettings[status] = ErrorHandling.UnknownHandling;
    }
  }

  public Dictionary<CommandStatusCode, ErrorHandling> CurrentErrorHandlingSettings { get; set; }

  [Reactive]
  public partial int ExperimentRetryLimit { get; set; }

  [Reactive]
  public partial int CommandRetryLimit { get; set; }

  [Reactive]
  public partial int ExperimentRetryCooldown { get; set; }

  [Reactive]
  public partial int CommandLatency { get; set; }
}
