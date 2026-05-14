using System.IO.Compression;
using Ares.Core.DataManagement.DataMappers;
using Ares.Datamodel;
using Ares.Datamodel.Extensions;

namespace Ares.Core.Tests.DataManagement.DataMappers;

internal class AresDatasetExporterTests
{
  [Test]
  public void ExportCsv_WritesHeadersInColumnOrder()
  {
    var dataset = CreateDataset("Device A",
      new AresDataColumn { Name = "Timestamp", DisplayName = "Time" },
      new AresDataColumn { Name = "Temperature" });

    dataset.Rows.Add(CreateRow(("Timestamp", AresValueHelper.CreateString("t0")), ("Temperature", AresValueHelper.CreateNumber(1.2))));

    var csv = ReadText(AresDatasetExporter.ExportCsv(dataset));

    Assert.That(csv.Split(Environment.NewLine)[0], Is.EqualTo("Time,Temperature"));
  }

  [Test]
  public void ExportCsv_WritesRowsAndLeavesMissingCellsEmpty()
  {
    var dataset = CreateDataset("Device A",
      new AresDataColumn { Name = "Timestamp" },
      new AresDataColumn { Name = "Temperature" },
      new AresDataColumn { Name = "Pressure" });

    dataset.Rows.Add(CreateRow(("Timestamp", AresValueHelper.CreateString("t0")), ("Temperature", AresValueHelper.CreateNumber(1.2))));

    var csv = ReadText(AresDatasetExporter.ExportCsv(dataset));

    Assert.That(csv.Split(Environment.NewLine)[1], Is.EqualTo("t0,1.2,"));
  }

  [Test]
  public void ExportCsv_UsesStringifyForValues()
  {
    var dataset = CreateDataset("Device A", new AresDataColumn { Name = "Quantity" });
    var quantity = AresValueHelper.CreateQuantity(4.5, QuantityType.Mass, "g");
    dataset.Rows.Add(CreateRow(("Quantity", quantity)));

    var csv = ReadText(AresDatasetExporter.ExportCsv(dataset));

    Assert.That(csv.Split(Environment.NewLine)[1], Is.EqualTo(quantity.Stringify()));
  }

  [Test]
  public void ExportZip_CreatesOneCsvEntryPerDataset()
  {
    var datasets = new[]
    {
      CreateDataset("Device A", new AresDataColumn { Name = "Value" }),
      CreateDataset("Device B", new AresDataColumn { Name = "Value" })
    };

    using var stream = AresDatasetExporter.ExportZip(datasets);
    using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

    Assert.That(archive.Entries.Select(entry => entry.FullName), Is.EqualTo(["Device A.csv", "Device B.csv"]));
  }

  [Test]
  public void ExportZip_SanitizesAndUniquifiesEntryNames()
  {
    var datasets = new[]
    {
      CreateDataset("Device:A", new AresDataColumn { Name = "Value" }),
      CreateDataset("Device:A", new AresDataColumn { Name = "Value" }),
      CreateDataset("   ", new AresDataColumn { Name = "Value" })
    };

    using var stream = AresDatasetExporter.ExportZip(datasets);
    using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

    Assert.That(archive.Entries.Select(entry => entry.FullName), Is.EqualTo(["Device_A.csv", "Device_A-2.csv", "dataset.csv"]));
  }

  private static AresDataset CreateDataset(string name, params AresDataColumn[] columns)
  {
    var dataset = new AresDataset
    {
      Name = name
    };

    dataset.Columns.AddRange(columns);
    return dataset;
  }

  private static AresDataRow CreateRow(params (string Name, AresValue Value)[] values)
  {
    var data = new AresStruct();
    foreach(var value in values)
    {
      data.Fields[value.Name] = value.Value;
    }

    return new AresDataRow
    {
      Data = data
    };
  }

  private static string ReadText(Stream stream)
  {
    using(stream)
    using(var reader = new StreamReader(stream))
    {
      return reader.ReadToEnd().TrimEnd();
    }
  }
}
