using Ares.Core.DataManagement.DataMappers;
using Ares.Datamodel;
using Ares.Datamodel.Extensions;
using Google.Protobuf.WellKnownTypes;

namespace Ares.Core.Tests.DataManagement.DataMappers;

internal class DeviceStateWideDatasetProjectorTests
{
  [Test]
  public void Project_CombinesMultipleDeviceDatasets()
  {
    var timestamp = Timestamp.FromDateTime(DateTime.UnixEpoch);
    var deviceDatasets = new[]
    {
      CreateDataset("Device A", CreateRow(timestamp, ("Temperature", AresValueHelper.CreateNumber(1)))),
      CreateDataset("Device B", CreateRow(timestamp, ("Pressure", AresValueHelper.CreateNumber(2))))
    };

    var wideDataset = DeviceStateWideDatasetProjector.Project(deviceDatasets);

    using(Assert.EnterMultipleScope())
    {
      Assert.That(wideDataset.Name, Is.EqualTo("Device State Wide"));
      Assert.That(wideDataset.Columns.Select(column => column.Name), Is.EqualTo([
        "Timestamp",
        "Device A.Temperature",
        "Device B.Pressure"
      ]));
      Assert.That(wideDataset.Rows, Has.Count.EqualTo(1));
      Assert.That(wideDataset.Rows[0].Data.Fields["Device A.Temperature"].NumberValue, Is.EqualTo(1));
      Assert.That(wideDataset.Rows[0].Data.Fields["Device B.Pressure"].NumberValue, Is.EqualTo(2));
    }
  }

  [Test]
  public void Project_PreservesFlattenedPropertyNames()
  {
    var timestamp = Timestamp.FromDateTime(DateTime.UnixEpoch);
    var deviceDatasets = new[]
    {
      CreateDataset("Pump", CreateRow(timestamp, ("Position.Offset.X", AresValueHelper.CreateNumber(1.2))))
    };

    var wideDataset = DeviceStateWideDatasetProjector.Project(deviceDatasets);

    Assert.That(wideDataset.Columns.Select(column => column.Name), Does.Contain("Pump.Position.Offset.X"));
  }

  [Test]
  public void Project_CreatesRowsForDistinctTimestampsInOrder()
  {
    var firstTimestamp = Timestamp.FromDateTime(DateTime.UnixEpoch);
    var secondTimestamp = Timestamp.FromDateTime(DateTime.UnixEpoch.AddSeconds(1));
    var deviceDatasets = new[]
    {
      CreateDataset("Device A", CreateRow(secondTimestamp, ("Temperature", AresValueHelper.CreateNumber(2)))),
      CreateDataset("Device B", CreateRow(firstTimestamp, ("Temperature", AresValueHelper.CreateNumber(1))))
    };

    var wideDataset = DeviceStateWideDatasetProjector.Project(deviceDatasets);

    Assert.That(wideDataset.Rows.Select(row => row.Data.Fields["Timestamp"].TimestampValue), Is.EqualTo([
      firstTimestamp,
      secondTimestamp
    ]));
  }

  [Test]
  public void Project_CarriesForwardLatestKnownValues()
  {
    var firstTimestamp = Timestamp.FromDateTime(DateTime.UnixEpoch);
    var secondTimestamp = Timestamp.FromDateTime(DateTime.UnixEpoch.AddSeconds(1));
    var thirdTimestamp = Timestamp.FromDateTime(DateTime.UnixEpoch.AddSeconds(2));
    var deviceDatasets = new[]
    {
      CreateDataset("Device A",
        CreateRow(firstTimestamp, ("Temperature", AresValueHelper.CreateNumber(1))),
        CreateRow(thirdTimestamp, ("Temperature", AresValueHelper.CreateNumber(3)))),
      CreateDataset("Device B",
        CreateRow(secondTimestamp, ("Pressure", AresValueHelper.CreateNumber(2))))
    };

    var wideDataset = DeviceStateWideDatasetProjector.Project(deviceDatasets);

    using(Assert.EnterMultipleScope())
    {
      Assert.That(wideDataset.Rows[1].Data.Fields["Device A.Temperature"].NumberValue, Is.EqualTo(1));
      Assert.That(wideDataset.Rows[2].Data.Fields["Device A.Temperature"].NumberValue, Is.EqualTo(3));
      Assert.That(wideDataset.Rows[2].Data.Fields["Device B.Pressure"].NumberValue, Is.EqualTo(2));
    }
  }

  [Test]
  public void Project_LeavesMissingPriorValuesEmpty()
  {
    var firstTimestamp = Timestamp.FromDateTime(DateTime.UnixEpoch);
    var secondTimestamp = Timestamp.FromDateTime(DateTime.UnixEpoch.AddSeconds(1));
    var deviceDatasets = new[]
    {
      CreateDataset("Device A", CreateRow(firstTimestamp, ("Temperature", AresValueHelper.CreateNumber(1)))),
      CreateDataset("Device B", CreateRow(secondTimestamp, ("Pressure", AresValueHelper.CreateNumber(2))))
    };

    var wideDataset = DeviceStateWideDatasetProjector.Project(deviceDatasets);

    Assert.That(wideDataset.Rows[0].Data.Fields.ContainsKey("Device B.Pressure"), Is.False);
  }

  [Test]
  public void Project_UniquifiesDuplicateColumnNames()
  {
    var timestamp = Timestamp.FromDateTime(DateTime.UnixEpoch);
    var deviceDatasets = new[]
    {
      CreateDataset("Device", CreateRow(timestamp, ("Value", AresValueHelper.CreateNumber(1)))),
      CreateDataset("Device", CreateRow(timestamp, ("Value", AresValueHelper.CreateNumber(2))))
    };

    var wideDataset = DeviceStateWideDatasetProjector.Project(deviceDatasets);

    Assert.That(wideDataset.Columns.Select(column => column.Name), Is.EqualTo([
      "Timestamp",
      "Device.Value",
      "Device.Value-2"
    ]));
  }

  [Test]
  public void Project_ThrowsWhenAlreadyCanceled()
  {
    var timestamp = Timestamp.FromDateTime(DateTime.UnixEpoch);
    var deviceDatasets = new[]
    {
      CreateDataset("Device A", CreateRow(timestamp, ("Temperature", AresValueHelper.CreateNumber(1))))
    };
    using var cancellationTokenSource = new CancellationTokenSource();
    cancellationTokenSource.Cancel();

    Assert.Throws<OperationCanceledException>(() => DeviceStateWideDatasetProjector.Project(deviceDatasets, cancellationTokenSource.Token));
  }

  [Test]
  public void Project_ThrowsWhenCanceledDuringProjection()
  {
    var rows = Enumerable.Range(0, 100_000)
      .Select(index => CreateRow(
        Timestamp.FromDateTime(DateTime.UnixEpoch.AddMilliseconds(index)),
        ("Temperature", AresValueHelper.CreateNumber(index))))
      .ToArray();
    var deviceDatasets = new[]
    {
      CreateDataset("Device A", rows)
    };
    using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(1));

    Assert.Throws<OperationCanceledException>(() => DeviceStateWideDatasetProjector.Project(deviceDatasets, cancellationTokenSource.Token));
  }

  private static AresDataset CreateDataset(string name, params AresDataRow[] rows)
  {
    var dataset = new AresDataset
    {
      Name = name
    };

    dataset.Columns.Add(new AresDataColumn
    {
      Name = "Timestamp",
      Schema = new AresValueSchema { Type = AresDataType.Timestamp }
    });

    foreach(var propertyName in rows
      .SelectMany(row => row.Data.Fields.Keys)
      .Where(fieldName => fieldName != "Timestamp")
      .Distinct())
    {
      dataset.Columns.Add(new AresDataColumn
      {
        Name = propertyName,
        Schema = new AresValueSchema { Type = AresDataType.Any, Optional = true }
      });
    }

    dataset.Rows.AddRange(rows);
    return dataset;
  }

  private static AresDataRow CreateRow(Timestamp timestamp, params (string Name, AresValue Value)[] values)
  {
    var data = new AresStruct();
    data.Fields["Timestamp"] = AresValueHelper.CreateTimestamp(timestamp);

    foreach(var value in values)
    {
      data.Fields[value.Name] = value.Value;
    }

    return new AresDataRow
    {
      Data = data
    };
  }
}
