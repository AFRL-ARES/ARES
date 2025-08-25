using Ares.Datamodel;
using Ares.Datamodel.Planning;
using Ares.Services;
using Grpc.Core;
using ReactiveUI;

namespace UI.Backend.ViewModels.Settings.Planning;

public class PlannerSettingsEditorViewModel : ReactiveObject
{
  private readonly AresPlannerManagementService.AresPlannerManagementServiceClient _client;
  private readonly PlannerServiceInfo _plannerInfo;

  public PlannerSettingsEditorViewModel(AresPlannerManagementService.AresPlannerManagementServiceClient client, PlannerServiceInfo plannerInfo)
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
      await _client.SetPlannerSettingsAsync(settings);
    }
    catch(RpcException)
    {

    }
  }

  public async Task FetchSettings()
  {
    var request = new PlannerSettingsRequest { PlannerId = _plannerInfo.UniqueId };
    try
    {
      var settingsResponse = await _client.GetPlannerSettingsAsync(request);
      Settings = settingsResponse;
    }
    catch(RpcException)
    {
      Settings = new AresStruct();
    }
  }

  public async Task UpdateInfo()
  {
    var request = new PlannerInfoRequest { PlannerId= _plannerInfo.UniqueId };
    var infoResponse = await _client.GetInfoAsync(request);
    SettingsSchema = infoResponse.Info.Capabilities?.SettingsSchema ?? new AresDataSchema();
  }

  public AresStruct Settings { get; set; } = new AresStruct();

  public AresDataSchema SettingsSchema { get; private set; } = new AresDataSchema();

  public bool Modified = true;
  public AresStruct Save()
    => Modified ? Settings : Settings;
}
