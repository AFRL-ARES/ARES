using Ares.Datamodel.Templates;
using Ares.Services.Device;

namespace UI.Backend.ViewModels.Automation.CampaignEdit.Factories;

public class MetadataPickerFactory
{
  private readonly AresDevices.AresDevicesClient _devicesClient;

  public MetadataPickerFactory(AresDevices.AresDevicesClient devicesClient)
  {
    _devicesClient = devicesClient;
  }

  public MetadataPickerViewModel Create()
    => new MetadataPickerViewModel(_devicesClient);

  public MetadataPickerViewModel Create(CommandMetadata? existingMetadata)
  {
    if (existingMetadata is null)
      return Create();

    return new MetadataPickerViewModel(existingMetadata, _devicesClient);
  }
}
