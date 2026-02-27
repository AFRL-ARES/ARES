using Ares.Services;
using Ares.Core.Grpc.Services;
using Microsoft.OpenApi;
using ReactiveUI;
using UI.Features.DeviceStateLogging;
using DeviceStateFilterViewModel=UI.Features.DeviceStateLogging.DeviceStateFilterViewModel;

namespace UI.Features.DeviceStateExport;

public class DeviceStateExporterViewModel : ReactiveObject
{
  readonly Ares.Core.Grpc.Services.DeviceStateExportService _stateExportServiceClient;

  public DeviceStateExporterViewModel(DeviceStateFilterViewModelFactory vmFactory,
    Ares.Core.Grpc.Services.DeviceStateExportService stateExportServiceClient)
  {
    _stateExportServiceClient = stateExportServiceClient;
    FilterViewModel = vmFactory.Create();
  }

  public string Error { get; private set; } = string.Empty;

  public ExportTypeWrapper SelectedExportType { get; set; }

  public DeviceStateFilterViewModel FilterViewModel { get; }

  public IEnumerable<ExportTypeWrapper> AvailableExportTypes { get; } = Enum.GetValues<ExportType>().Select(t => new ExportTypeWrapper(t));

  public async Task<byte[]> GetExportStream()
  {
    if(SelectedExportType.ExportType == ExportType.Unspecified)
      return Array.Empty<byte>();

    var filter = FilterViewModel.GetStateRequestFilter();
    var request = new DeviceStateRequest()
    {
      Filter = filter,
      ExportType = SelectedExportType.ExportType
    };

    var result = await _stateExportServiceClient.GetStateExport(request, null);

    return result.Data.ToArray();
  }
}

public readonly struct ExportTypeWrapper
{
  public ExportTypeWrapper(ExportType type)
  {
    ExportType = type;
    Name = type.GetDisplayName();
  }

  public string Name { get; }
  public ExportType ExportType { get; }
}
