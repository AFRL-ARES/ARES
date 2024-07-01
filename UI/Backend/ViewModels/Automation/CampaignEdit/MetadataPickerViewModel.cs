using Ares.Messaging;
using Ares.Messaging.Device;
using Google.Protobuf.WellKnownTypes;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace UI.Backend.ViewModels.Automation.CampaignEdit;

public class MetadataPickerViewModel : ReactiveObject
{
  private readonly AresDevices.AresDevicesClient _devicesClient;
  private readonly Guid _existingGuid = Guid.NewGuid();
  private Task _deviceRefreshTask = Task.CompletedTask;
  private CommandMetadata? _selectedCommandMetadata;
  private AresDeviceInfo? _selectedDevice;
  private string? _selectedDeviceName;
  private string? _selectedMetadataName;

  public MetadataPickerViewModel(AresDevices.AresDevicesClient devicesClient)
  {
    _devicesClient = devicesClient;
  }

  public MetadataPickerViewModel(CommandMetadata existingMetadata, AresDevices.AresDevicesClient devicesClient) : this(devicesClient)
  {
    _existingGuid = Guid.Parse(existingMetadata.UniqueId);
    _selectedDeviceName = existingMetadata.DeviceName;
    _selectedMetadataName = existingMetadata.Name;
    _ = RetrieveInfoForExistingMeta();
  }

  public IEnumerable<AresDeviceInfo> AvailableDevices { get; private set; } = Array.Empty<AresDeviceInfo>();

  [Reactive]
  public IEnumerable<CommandMetadata> AvailableMetadata { get; private set; } = Array.Empty<CommandMetadata>();

  public string? SelectedDeviceName
  {
    get => _selectedDeviceName;

    set
    {
      _selectedDeviceName = value;
      SelectedDevice = AvailableDevices.FirstOrDefault(info => info.Name == value);
      if (SelectedDevice is null)
        return;

      _ = RefreshMetadata();
    }
  }

  [Reactive]
  public string? SelectedMetadataDescription { get; set; }


  public string? SelectedMetadataName
  {
    get => _selectedMetadataName;

    set
    {
      _selectedMetadataName = value;
      if (string.IsNullOrEmpty(value))
      {
        SelectedCommandMetadata = null;
        SelectedMetadataDescription = null;
        return;
      }


      var selectedCommandMetadata = AvailableMetadata.FirstOrDefault(metadata => metadata.DeviceName == SelectedDevice?.Name && metadata.Name == value);
      if (selectedCommandMetadata is not null)
        selectedCommandMetadata.UniqueId = _existingGuid.ToString();

      SelectedCommandMetadata = selectedCommandMetadata;
    }
  }

  public CommandMetadata? SelectedCommandMetadata
  {
    get => _selectedCommandMetadata;

    set
    {
      _selectedCommandMetadata = value;
      this.RaisePropertyChanged();
      if (value is null)
        return;

      SelectedMetadataDescription = value.Description;
    }
  }

  public AresDeviceInfo? SelectedDevice
  {
    get => _selectedDevice;

    private set => this.RaiseAndSetIfChanged(ref _selectedDevice, value);
  }

  public CommandMetadata? Save()
    => SelectedCommandMetadata;

  public Task Reset()
  {
    AvailableMetadata = Array.Empty<CommandMetadata>();
    AvailableDevices = Array.Empty<AresDeviceInfo>();
    return RefreshDevices();
  }

  public async Task RefreshDevices()
  {
    var devicesResponse = await _devicesClient.ListAresDevicesAsync(new Empty());
    AvailableDevices = devicesResponse.AresDevices.ToArray();
  }

  public async Task RefreshMetadata()
  {
    if (SelectedDevice is null)
    {
      AvailableMetadata = Array.Empty<CommandMetadata>();
      return;
    }

    var request = new CommandMetadatasRequest { DeviceName = SelectedDevice.Name };
    var metadataResponse = await _devicesClient.GetCommandMetadatasAsync(request);
    AvailableMetadata = metadataResponse.Metadatas.ToArray();
  }

  private async Task RetrieveInfoForExistingMeta()
  {
    //await _deviceRefreshTask;
    var device = AvailableDevices.FirstOrDefault(info => info.Name == SelectedDeviceName);
    if (device is null)
      return;

    SelectedDevice = device;
    await RefreshMetadata();
    var metadata = AvailableMetadata.FirstOrDefault(info => info.Name == SelectedMetadataName);
    if (metadata is null)
      return;

    SelectedCommandMetadata = metadata;
  }
}
