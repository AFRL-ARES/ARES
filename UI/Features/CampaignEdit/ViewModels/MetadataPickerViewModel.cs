using Ares.Datamodel.Device;
using Ares.Datamodel.Templates;
using Ares.Services.Device;
using Ares.Core.Grpc.Services;
using Google.Protobuf.WellKnownTypes;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace UI.Features.CampaignEdit.ViewModels;

public partial class MetadataPickerViewModel : ReactiveObject
{
  private readonly DevicesService _devicesClient;
  private Task _deviceRefreshTask = Task.CompletedTask;

  public MetadataPickerViewModel(DevicesService devicesClient)
  {
    _devicesClient = devicesClient;
    AvailableDevices = [];
    AvailableMetadata = [];
  }

  public MetadataPickerViewModel(CommandMetadata existingMetadata, DevicesService devicesClient) : this(devicesClient)
  {
    SelectedCommandMetadata = existingMetadata;
    AvailableDevices = [];
    AvailableMetadata = [];
  }

  [Reactive]
  public partial IEnumerable<DeviceInfo> AvailableDevices { get; private set; }

  [Reactive]
  public partial IEnumerable<CommandMetadata> AvailableMetadata { get; private set; }

  [Reactive]
  public partial CommandMetadata? SelectedCommandMetadata { get; set; }

  [Reactive]
  public partial DeviceInfo? SelectedDevice { get; set; }

  public CommandMetadata? Save()
    => SelectedCommandMetadata;

  public async Task Reset()
  {
    AvailableMetadata = Array.Empty<CommandMetadata>();
    AvailableDevices = Array.Empty<DeviceInfo>();
    await RefreshDevices();
  }

  public async Task SelectDevice(string deviceId)
  {
    var device = AvailableDevices.FirstOrDefault(d => d.UniqueId == deviceId);
    SelectedDevice = device;
    await RefreshMetadata();
  }

  public void SelectMetadata(string metadataName)
  {
    var meta = AvailableMetadata.FirstOrDefault(m => m.Name == metadataName);
    SelectedCommandMetadata = meta;
  }

  public async Task RefreshDevices()
  {
    var devicesResponse = await _devicesClient.ListAresDevices(new Empty(), null);
    AvailableDevices = devicesResponse.AresDevices.ToArray();
    if (SelectedCommandMetadata is not null)
    {
      SelectedDevice = AvailableDevices.FirstOrDefault(d => d.UniqueId == SelectedCommandMetadata.DeviceId);
      await RefreshMetadata();
    }
  }

  public async Task RefreshMetadata()
  {
    if(SelectedDevice is null)
    {
      AvailableMetadata = Array.Empty<CommandMetadata>();
      SelectedCommandMetadata = null;
      return;
    }

    var request = new CommandMetadatasRequest { DeviceId = SelectedDevice.UniqueId };
    var metadataResponse = await _devicesClient.GetCommandMetadatas(request, null);
    AvailableMetadata = metadataResponse.Metadatas.ToArray();

    if (SelectedCommandMetadata is null || SelectedCommandMetadata.DeviceId == SelectedDevice.UniqueId)
    {
      return;
    }

    SelectedCommandMetadata = null;
  }
}
