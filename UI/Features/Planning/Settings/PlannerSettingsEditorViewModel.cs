using Ares.Datamodel;
using Ares.Datamodel.Planning;
using Ares.Services;
using Ares.Core.Grpc.Services;
using Grpc.Core;
using ReactiveUI;

namespace UI.Features.Planning.Settings;

public class PlannerSettingsEditorViewModel : ReactiveObject
{
  private readonly PlannerService _client;
  private readonly PlannerServiceInfo _plannerInfo;

  public PlannerSettingsEditorViewModel(PlannerService client, PlannerServiceInfo plannerInfo)
  {
    _client = client;
    _plannerInfo = plannerInfo;
  }

  public async Task PushSettings()
  {
    var settings = new PlannerSettings
    {
      PlannerId = _plannerInfo.UniqueId,
      Settings = Settings
    };

    try
    {
      await _client.SetPlannerSettings(settings, null);
    }
    catch(Exception)
    {

    }
  }

  public async Task FetchSettings()
  {
    var request = new PlannerSettingsRequest { PlannerId = _plannerInfo.UniqueId };
    try
    {
      var settingsResponse = await _client.GetPlannerSettings(request, null);
      Settings = settingsResponse;
    }
    catch(Exception)
    {
      Settings = new AresStruct();
    }
  }

  public async Task UpdateInfo()
  {
    var request = new PlannerInfoRequest { PlannerId= _plannerInfo.UniqueId };
    var infoResponse = await _client.GetInfo(request, null);
    SettingsSchema = infoResponse.Info.Capabilities?.SettingsSchema ?? new AresStructSchema();
  }

  public AresStruct Settings { get; set; } = new AresStruct();

  public AresStructSchema SettingsSchema { get; private set; } = new AresStructSchema();

  public bool Modified = true;
  public AresStruct Save()
    => Modified ? Settings : Settings;
}
