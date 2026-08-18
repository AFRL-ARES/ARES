using System.Globalization;
using Ares.Core.DataManagement.DataMappers;
using Ares.Datamodel;
using Ares.Datamodel.Device;
using CsvHelper.Configuration;

namespace Ares.Core.Device.State.Export.ExportStreamProviders;

/// <summary>
/// Crafts a zip file of all relevant device state data
/// </summary>
public class DeviceExportStreamProvider : IDeviceStateExportStreamProvider
{
  readonly DeviceStateDatasetGenerator _stateGetter;

  public DeviceExportStreamProvider(DeviceStateDatasetGenerator dataProviders)
  {
    _stateGetter = dataProviders;
  }

  public async Task<MemoryStream> Export(DeviceStateRequestFilter request)
  {
    var config = new CsvConfiguration(CultureInfo.InvariantCulture)
    {
      NewLine = Environment.NewLine
    };

    var exportData = await GetStateExportData(request);

    var stream = AresDatasetExporter.ExportZip(exportData);
    return stream;
  }

  private async Task<AresDataset[]> GetStateExportData(DeviceStateRequestFilter request)
  {
    var dataProviderGetters = await _stateGetter.GenerateAsync(request);
    return dataProviderGetters;
  }
}
