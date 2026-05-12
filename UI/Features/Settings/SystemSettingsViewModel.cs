using Ares.Core.Settings;
using Ares.Datamodel;
using ReactiveUI;

namespace UI.Features.Settings;

public class SystemSettingsViewModel : ReactiveObject
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
    var configs = CurrentErrorHandlingSettings.Select(kvp => new DeviceErrorHandlingConfig() { Code = kvp.Key, Handling = kvp.Value }).ToList();
    await _settingsManager.UpdateErrorHandlingSettings(configs);
  }

  public async Task GetUpdatedSettings()
  {
    var newSettings = await _settingsManager.GetCurrentErrorHandlingSettings();
    foreach(var setting in newSettings)
      CurrentErrorHandlingSettings[setting.Code] = setting.Handling;
  }

  private void Initialize()
  {
    foreach(var status in Enum.GetValues<CommandStatusCode>())
    {
      if(!CurrentErrorHandlingSettings.TryGetValue(status, out var val))
        CurrentErrorHandlingSettings[status] = ErrorHandling.UnknownHandling;
    }
  }


  public Dictionary<CommandStatusCode, ErrorHandling> CurrentErrorHandlingSettings { get; set; }
}
