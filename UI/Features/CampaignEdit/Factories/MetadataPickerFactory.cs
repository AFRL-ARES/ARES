using Ares.Datamodel.Templates;
using Ares.Services.Device;
using Ares.Core.Grpc.Services;
using MetadataPickerViewModel=UI.Features.CampaignEdit.ViewModels.MetadataPickerViewModel;

namespace UI.Features.CampaignEdit.Factories;

public class MetadataPickerFactory
{
  private readonly DevicesService _devicesClient;

  public MetadataPickerFactory(DevicesService devicesClient)
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
