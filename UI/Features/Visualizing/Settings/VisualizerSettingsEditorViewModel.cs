using Ares.Core.Grpc.Services;
using Ares.Datamodel;
using Ares.Datamodel.Analyzing;
using Ares.Datamodel.Visualizing;
using Grpc.Core;
using ReactiveUI;
using Ares.Services;

namespace UI.Features.Visualizing.Settings;

public class VisualizerSettingsEditorViewModel : ReactiveObject
{
  private readonly VisualizerService _client;
  private readonly VisualizerInfo _visualizerInfo;

  public VisualizerSettingsEditorViewModel(VisualizerService client, VisualizerInfo visualizerInfo)
  {
    _client = client;
    _visualizerInfo = visualizerInfo;
  }

  public AresStruct Settings { get; private set; } = new();
  public AresStructSchema SettingsSchema { get; private set; } = new();
  public bool Modified { get; private set; } = true;

  public async Task PushSettings()
  {
    try
    {
      await _client.SetVisualizerSettings(new VisualizerSettings
      {
        VisualizerId = _visualizerInfo.UniqueId,
        Settings = Settings
      }, null);
    }
    catch(RpcException)
    {
      // Settings are also pushed on dialog confirmation; leave the editor usable when a live push fails.
    }
  }

  public async Task FetchSettings()
  {
    try
    {
      Settings = await _client.GetVisualizerSettings(new VisualizerSettingsRequest
      {
        VisualizerId = _visualizerInfo.UniqueId
      }, null);
    }
    catch(Exception)
    {
      Settings = new AresStruct();
    }
  }

  public async Task UpdateInfo()
  {
    SettingsSchema = await _client.GetVisualizerSettingsSchema(_visualizerInfo.UniqueId);
  }
}
