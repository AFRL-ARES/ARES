using Ares.Core.DataManagement.DataMappers;
using Ares.Datamodel;
using Ares.Datamodel.Extensions;
using Google.Protobuf.WellKnownTypes;

namespace Ares.Core.Tests.DataManagement.DataMappers;

internal class DeviceStateLongDatasetProjectorTests
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

    var longDataset = DeviceStateLongDatasetProjector.Project(deviceDatasets);

    using(Assert.EnterMultipleScope())
    {
      Assert.That(longDataset.Columns.Select(column => column.Name), Is.EqualTo([
        "Timestamp",
        "Campaign",
        "Experiment Number",
        "Step Name",
        "Device",
        "Property",
        "Value",
        "Unit"
      ]));
      Assert.That(longDataset.Rows.Select(row => row.Data.Fields["Device"].StringValue), Is.EqualTo(["Device A", "Device B"]));
      Assert.That(longDataset.Rows.Select(row => row.Data.Fields["Property"].StringValue), Is.EqualTo(["Temperature", "Pressure"]));
      Assert.That(longDataset.Rows.Select(row => row.Data.Fields["Value"].NumberValue), Is.EqualTo([1, 2]));
    }
  }

  [Test]
  public void Project_CopiesExecutionInformationOntoPropertyRows()
  {
    var timestamp = Timestamp.FromDateTime(DateTime.UnixEpoch);
    var deviceDatasets = new[]
    {
      CreateDataset(
        "Device A",
        CreateRow(
          timestamp,
          ("Campaign", AresValueHelper.CreateString("Campaign A")),
          ("Experiment Number", AresValueHelper.CreateInt(2)),
          ("Step Name", AresValueHelper.CreateString("Heat")),
          ("Temperature", AresValueHelper.CreateNumber(1)),
          ("Pressure", AresValueHelper.CreateNumber(2))))
    };

    var longDataset = DeviceStateLongDatasetProjector.Project(deviceDatasets);

    using(Assert.EnterMultipleScope())
    {
      Assert.That(longDataset.Rows.Select(row => row.Data.Fields["Property"].StringValue), Is.EqualTo(["Temperature", "Pressure"]));
      Assert.That(longDataset.Rows.All(row => row.Data.Fields["Campaign"].StringValue == "Campaign A"), Is.True);
      Assert.That(longDataset.Rows.All(row => row.Data.Fields["Experiment Number"].IntValue == 2), Is.True);
      Assert.That(longDataset.Rows.All(row => row.Data.Fields["Step Name"].StringValue == "Heat"), Is.True);
    }
  }

  [Test]
  public void Project_LeavesExecutionInformationBlankWhenSourceRowDoesNotContainIt()
  {
    var timestamp = Timestamp.FromDateTime(DateTime.UnixEpoch);
    var deviceDatasets = new[]
    {
      CreateDataset("Device A", CreateRow(timestamp, ("Temperature", AresValueHelper.CreateNumber(1))))
    };

    var row = DeviceStateLongDatasetProjector.Project(deviceDatasets).Rows.Single();

    using(Assert.EnterMultipleScope())
    {
      Assert.That(row.Data.Fields.ContainsKey("Campaign"), Is.False);
      Assert.That(row.Data.Fields.ContainsKey("Experiment Number"), Is.False);
      Assert.That(row.Data.Fields.ContainsKey("Step Name"), Is.False);
    }
  }

  [Test]
  public void Project_SplitsQuantityValueAndUnit()
  {
    var timestamp = Timestamp.FromDateTime(DateTime.UnixEpoch);
    var deviceDatasets = new[]
    {
      CreateDataset("Device A", CreateRow(timestamp, ("Mass", AresValueHelper.CreateQuantity(4.5, QuantityType.Mass, "g"))))
    };

    var row = DeviceStateLongDatasetProjector.Project(deviceDatasets).Rows.Single();

    using(Assert.EnterMultipleScope())
    {
      Assert.That(row.Data.Fields["Value"].KindCase, Is.EqualTo(AresValue.KindOneofCase.NumberValue));
      Assert.That(row.Data.Fields["Value"].NumberValue, Is.EqualTo(4.5));
      Assert.That(row.Data.Fields["Unit"].StringValue, Is.EqualTo("g"));
    }
  }

  [Test]
  public void Project_OmitsUnitForNonQuantityValues()
  {
    var timestamp = Timestamp.FromDateTime(DateTime.UnixEpoch);
    var deviceDatasets = new[]
    {
      CreateDataset("Device A", CreateRow(timestamp, ("Enabled", AresValueHelper.CreateBool(true))))
    };

    var row = DeviceStateLongDatasetProjector.Project(deviceDatasets).Rows.Single();

    using(Assert.EnterMultipleScope())
    {
      Assert.That(row.Data.Fields["Value"].BoolValue, Is.True);
      Assert.That(row.Data.Fields.ContainsKey("Unit"), Is.False);
    }
  }

  [Test]
  public void Project_PreservesFlattenedPropertyNames()
  {
    var timestamp = Timestamp.FromDateTime(DateTime.UnixEpoch);
    var deviceDatasets = new[]
    {
      CreateDataset("Device A", CreateRow(timestamp, ("Position.Offset.X", AresValueHelper.CreateNumber(1.2))))
    };

    var row = DeviceStateLongDatasetProjector.Project(deviceDatasets).Rows.Single();

    Assert.That(row.Data.Fields["Property"].StringValue, Is.EqualTo("Position.Offset.X"));
  }

  [Test]
  public void Project_DoesNotEmitTimestampAsProperty()
  {
    var timestamp = Timestamp.FromDateTime(DateTime.UnixEpoch);
    var deviceDatasets = new[]
    {
      CreateDataset("Device A", CreateRow(timestamp, ("Temperature", AresValueHelper.CreateNumber(1))))
    };

    var longDataset = DeviceStateLongDatasetProjector.Project(deviceDatasets);

    Assert.That(longDataset.Rows.Select(row => row.Data.Fields["Property"].StringValue), Does.Not.Contain("Timestamp"));
  }

  [Test]
  public void Project_SortsRowsByTimestampAcrossDevices()
  {
    var firstTimestamp = Timestamp.FromDateTime(DateTime.UnixEpoch);
    var secondTimestamp = Timestamp.FromDateTime(DateTime.UnixEpoch.AddSeconds(1));
    var deviceDatasets = new[]
    {
      CreateDataset("Device A", CreateRow(secondTimestamp, ("Temperature", AresValueHelper.CreateNumber(2)))),
      CreateDataset("Device B", CreateRow(firstTimestamp, ("Temperature", AresValueHelper.CreateNumber(1))))
    };

    var longDataset = DeviceStateLongDatasetProjector.Project(deviceDatasets);

    using(Assert.EnterMultipleScope())
    {
      Assert.That(longDataset.Rows.Select(row => row.Data.Fields["Timestamp"].TimestampValue), Is.EqualTo([
        firstTimestamp,
        secondTimestamp
      ]));
      Assert.That(longDataset.Rows.Select(row => row.Data.Fields["Device"].StringValue), Is.EqualTo(["Device B", "Device A"]));
    }
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

    Assert.Throws<OperationCanceledException>(() => DeviceStateLongDatasetProjector.Project(deviceDatasets, cancellationTokenSource.Token));
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

    Assert.Throws<OperationCanceledException>(() => DeviceStateLongDatasetProjector.Project(deviceDatasets, cancellationTokenSource.Token));
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
