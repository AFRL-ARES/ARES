using Ares.Datamodel.Templates;
using Ares.Core.Grpc.Services;
using MetadataPickerViewModel=UI.Features.CampaignEdit.ViewModels.MetadataPickerViewModel;
using Ares.Core.Device.Providers;

namespace UI.Features.CampaignEdit.Factories;

public class MetadataPickerFactory
{
  private readonly DevicesService _devicesClient;
  private readonly IAresDeviceProvider _deviceProvider;

  public MetadataPickerFactory(DevicesService devicesClient, IAresDeviceProvider deviceProvider)
  {
    _devicesClient = devicesClient;
    _deviceProvider = deviceProvider;
  }

  public MetadataPickerViewModel Create()
    => new MetadataPickerViewModel(_devicesClient, _deviceProvider);

  public MetadataPickerViewModel Create(CommandMetadata? existingMetadata)
  {
    if (existingMetadata is null)
      return Create();

    return new MetadataPickerViewModel(existingMetadata, _devicesClient, _deviceProvider);
  }
}
