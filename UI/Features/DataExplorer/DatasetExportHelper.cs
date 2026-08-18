using System.Globalization;
using Ares.Core.DataManagement.DataMappers;
using Ares.Datamodel;
using Microsoft.JSInterop;

namespace UI.Features.DataExplorer;

public static class DatasetExportHelper
{
  public static DatasetExport CreateCsv(AresDataset dataset, string fileNamePrefix)
  {
    return new DatasetExport(
      AresDatasetExporter.ExportCsv(dataset),
      $"{fileNamePrefix}-{CreateTimestamp()}.csv");
  }

  public static DatasetExport CreateZip(IEnumerable<AresDataset> datasets, string fileNamePrefix)
  {
    return new DatasetExport(
      AresDatasetExporter.ExportZip(datasets),
      $"{fileNamePrefix}-{CreateTimestamp()}.zip");
  }

  public static async Task DownloadAsync(IJSRuntime js, DatasetExport export)
  {
    using(export.Stream)
    using(var streamReference = new DotNetStreamReference(export.Stream))
    {
      await js.InvokeVoidAsync("downloadFileFromStream", export.FileName, streamReference);
    }
  }

  private static string CreateTimestamp()
  {
    return DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
  }
}