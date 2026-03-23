using Ares.Datamodel;
using Ares.Datamodel.Analyzing;
using Ares.Services;
using Ares.Core.Grpc.Services;
using Grpc.Core;
using ReactiveUI;

namespace UI.Features.Analyzing.Settings;

public class AnalyzerSettingsEditorViewModel : ReactiveObject
{
  private readonly AnalyzerService _client;
  private readonly AnalyzerInfo _analyzerInfo;

  public AnalyzerSettingsEditorViewModel(AnalyzerService client, AnalyzerInfo analyzerInfo)
  {
    _client = client;
    _analyzerInfo = analyzerInfo;
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
      await _client.SetAnalyzerSettings(settings, null);
    }
    catch(Exception)
    {
      // TODO maybe notify user
    }
  }

  public async Task FetchSettings()
  {
    var request = new AnalyzerSettingsRequest() { AnalyzerId = _analyzerInfo.UniqueId };
    try
    {
      var settingsResponse = await _client.GetAnalyzerSettings(request, null);
      Settings = settingsResponse;
    }
    catch(Exception)
    {
      Settings = new AresStruct();
    }
  }

  public async Task UpdateInfo()
  {
    var request = new AnalyzerInfoRequest { AnalyzerId = _analyzerInfo.UniqueId };
    var infoResponse = await _client.GetInfo(request, null);
    SettingsSchema = infoResponse.Info.Capabilities?.SettingsSchema ?? new AresStructSchema();
  }

  public AresStruct Settings { get; set; } = new AresStruct();
  public AresStructSchema SettingsSchema { get; private set; } = new AresStructSchema();
  public bool Modified = true;
  public AresStruct Save()
    => Modified ? Settings : Settings;
}
