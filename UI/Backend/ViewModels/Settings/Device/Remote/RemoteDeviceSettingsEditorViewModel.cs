using Ares.Datamodel;
using Ares.Datamodel.Device;
using Ares.Services.Device;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using ReactiveUI;

namespace UI.Backend.ViewModels.Settings.Device.Remote;

public class RemoteDeviceSettingsEditorViewModel : ReactiveObject
{
  private readonly AresDevices.AresDevicesClient _deviceClient;
  private readonly RemoteDeviceConfig _deviceConfig;

  public RemoteDeviceSettingsEditorViewModel(AresDevices.AresDevicesClient deviceClient, RemoteDeviceConfig deviceConfig)
  {
    _deviceClient = deviceClient;
    _deviceConfig = deviceConfig;
  }

  public async Task PushSettings()
  {
    try
    {
      var request = new DeviceSettings() { DeviceId = _deviceConfig.UniqueId, Settings = Settings };
      await _deviceClient.SetDeviceSettingsAsync(request);
    }
    catch(RpcException)
    {

    }
  }

  public async Task FetchSettings()
  {
    try
    {
      var request = new DeviceSettingsRequest() { DeviceId = _deviceConfig.UniqueId };
      var response = await _deviceClient.GetDeviceSettingsAsync(request);
      Settings = response;
    }

    catch(RpcException)
    {
      Settings = new AresStruct();
    }
  }

  public async Task UpdateInfo()
  {
    var request = new DeviceInfoRequest() { DeviceId = _deviceConfig.UniqueId };
    var response = await _deviceClient.GetDeviceInfoAsync(request);
    SettingsSchema = response.SettingsSchema;
  }

  public AresStruct Settings { get; set; } = new AresStruct();

  public AresDataSchema SettingsSchema { get; private set; } = new AresDataSchema();

  public bool Modified = true;
  public AresStruct Save()
    => Modified ? Settings : Settings;
}
