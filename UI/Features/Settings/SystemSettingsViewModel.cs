using Ares.Core.Settings;
using Ares.Datamodel;
using Google.Protobuf.WellKnownTypes;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace UI.Features.Settings;

public partial class SystemSettingsViewModel : ReactiveObject
{
  private readonly ISystemSettingsManager _settingsManager;

  public SystemSettingsViewModel(ISystemSettingsManager settingsManager)
  {
    _settingsManager = settingsManager;
    CurrentErrorHandlingSettings = new();
    Initialize();
  }

  public async Task PushUpdatedSettings()
  {
    var configs = CurrentErrorHandlingSettings.Select(kvp => new DeviceErrorHandlingConfig() 
    { 
      Code = kvp.Key, 
      Handling = kvp.Value }
    ).ToList();

    await _settingsManager.UpdateErrorHandlingSettings(configs);
    await _settingsManager.UpdateAresGeneralSettings(new AresGeneralSettingsConfig
    {
      ExperimentRetryLimit = ExperimentRetryLimit,
      RetryCooldown = new Duration() { Seconds = ExperimentRetryCooldown },
      CommandLatency = new Duration() { Seconds = CommandLatency },
      CommandRetryLimit = CommandRetryLimit
    });
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
