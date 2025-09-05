using Ares.Messages.DeviceState;
using Microsoft.OpenApi.Extensions;
using ReactiveUI;

namespace UI.Backend.ViewModels.DeviceStateLogging;

public class DeviceStateExporterViewModel : ReactiveObject
{
  readonly StateExportService.StateExportServiceClient _stateExportServiceClient;

  public DeviceStateExporterViewModel(DeviceStateFilterViewModelFactory vmFactory,
    StateExportService.StateExportServiceClient stateExportServiceClient)
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
    var request = new StateRequest()
    {
      Filter = filter,
      ExportType = SelectedExportType.ExportType
    };

    var result = await _stateExportServiceClient.GetStateExportAsync(request);

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
