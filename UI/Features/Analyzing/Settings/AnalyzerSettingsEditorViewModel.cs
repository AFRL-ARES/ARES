using Ares.Datamodel;
using Ares.Datamodel.Analyzing;
using Ares.Services;
using Grpc.Core;
using ReactiveUI;

namespace UI.Features.Analyzing.Settings;

public class AnalyzerSettingsEditorViewModel : ReactiveObject
{
  private readonly AresAnalyzerManagementService.AresAnalyzerManagementServiceClient _client;
  private readonly AnalyzerInfo _analyzerInfo;

  public AnalyzerSettingsEditorViewModel(AresAnalyzerManagementService.AresAnalyzerManagementServiceClient client, AnalyzerInfo analyzerInfo)
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
      await _client.SetAnalyzerSettingsAsync(settings);
    }
    catch(RpcException)
    {
      // TODO maybe notify user
    }
  }

  public async Task FetchSettings()
  {
    var request = new AnalyzerSettingsRequest() { AnalyzerId = _analyzerInfo.UniqueId };
    try
    {
      var settingsResponse = await _client.GetAnalyzerSettingsAsync(request);
      Settings = settingsResponse;
    }
    catch(RpcException)
    {
      Settings = new AresStruct();
    }
  }

  public async Task UpdateInfo()
  {
    var request = new AnalyzerInfoRequest { AnalyzerId = _analyzerInfo.UniqueId };
    var infoResponse = await _client.GetInfoAsync(request);
    SettingsSchema = infoResponse.Info.Capabilities?.SettingsSchema ?? new AresDataSchema();
  }

  public AresStruct Settings { get; set; } = new AresStruct();
  public AresDataSchema SettingsSchema { get; private set; } = new AresDataSchema();
  public bool Modified = true;
  public AresStruct Save()
    => Modified ? Settings : Settings;
}
