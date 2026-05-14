using System.Globalization;
using System.IO.Compression;
using Ares.Datamodel;
using Ares.Datamodel.Extensions;
using CsvHelper;
using CsvHelper.Configuration;

namespace Ares.Core.DataManagement.DataMappers;

public static class AresDatasetExporter
{
  public static MemoryStream ExportCsv(AresDataset dataset)
  {
    var stream = new MemoryStream();
    var config = new CsvConfiguration(CultureInfo.InvariantCulture)
    {
      NewLine = Environment.NewLine
    };

    using(var writer = new StreamWriter(stream, leaveOpen: true))
    using(var csv = new CsvWriter(writer, config))
    {
      WriteDataset(csv, dataset);
      writer.Flush();
    }

    stream.Position = 0;
    return stream;
  }

  public static MemoryStream ExportZip(IEnumerable<AresDataset> datasets)
  {
    var stream = new MemoryStream();
    var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    using(var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
    {
      foreach(var dataset in datasets)
      {
        var entry = archive.CreateEntry(GetUniqueCsvFileName(dataset.Name, usedNames));
        using var entryStream = entry.Open();
        using var datasetStream = ExportCsv(dataset);
        datasetStream.CopyTo(entryStream);
      }
    }

    stream.Position = 0;
    return stream;
  }

  private static void WriteDataset(CsvWriter csv, AresDataset dataset)
  {
    foreach(var column in dataset.Columns)
    {
      csv.WriteField(GetColumnTitle(column));
    }

    csv.NextRecord();

    foreach(var row in dataset.Rows)
    {
      foreach(var column in dataset.Columns)
      {
        var value = row.Data?.Fields.TryGetValue(column.Name, out var fieldValue) == true
          ? fieldValue.Stringify()
          : string.Empty;

        csv.WriteField(value);
      }

      csv.NextRecord();
    }
  }

  private static string GetColumnTitle(AresDataColumn column)
  {
    return column.HasDisplayName && !string.IsNullOrWhiteSpace(column.DisplayName)
      ? column.DisplayName
      : column.Name;
  }

  private static string GetUniqueCsvFileName(string name, HashSet<string> usedNames)
  {
    var baseName = SanitizeFileName(name);
    var fileName = $"{baseName}.csv";
    var index = 2;

    while(!usedNames.Add(fileName))
    {
      fileName = $"{baseName}-{index}.csv";
      index++;
    }

    return fileName;
  }

  private static string SanitizeFileName(string name)
  {
    var invalidCharacters = Path.GetInvalidFileNameChars();
    var sanitizedName = new string(name
      .Select(character => invalidCharacters.Contains(character) ? '_' : character)
      .ToArray()).Trim();

    return string.IsNullOrWhiteSpace(sanitizedName) ? "dataset" : sanitizedName;
  }
}
