using Ares.Messaging.Device;
using ReactiveUI;
using UI.Backend.DeviceStateExport.ExportStreamProviders;

namespace UI.Backend.ViewModels.DeviceStateLogging;

public class DeviceStateExporterViewModel : ReactiveObject
{
  readonly AresDevices.AresDevicesClient _devicesClient;

  public DeviceStateExporterViewModel(DeviceStateFilterViewModelFactory vmFactory,
    AresDevices.AresDevicesClient devicesClient,
    IEnumerable<IDeviceStateExportStreamProvider> exportStreamProviders)
  {
    ExportProviders = exportStreamProviders;
    _devicesClient = devicesClient;
    FilterViewModel = vmFactory.Create();
    SelectedExportProvider = ExportProviders.FirstOrDefault();
  }

  public string Error { get; private set; } = string.Empty;

  public IEnumerable<IDeviceStateExportStreamProvider> ExportProviders { get; set; }

  public IDeviceStateExportStreamProvider? SelectedExportProvider { get; set; }

  public DeviceStateFilterViewModel FilterViewModel { get; }

  public async Task<ExportStateStream?> GetExportStream()
  {
    var request = FilterViewModel.GetStateRequest();
    if (SelectedExportProvider is null)
      return null;

    return await SelectedExportProvider.Export(request);
  }
}
