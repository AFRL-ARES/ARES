using Ares.Core.DataManagement.DataMappers;
using Ares.Core.Device.State.Export.StateGetters;
using Ares.Datamodel;
using Ares.Datamodel.Device;
using Ares.Datamodel.Extensions;
using Google.Protobuf.WellKnownTypes;
using Moq;

namespace Ares.Core.Tests.DataManagement.DataMappers;

internal class DeviceStateDatasetGeneratorTests
{
  [Test]
  public async Task GenerateAsync_CreatesOneDatasetPerDevice()
  {
    var filter = new DeviceStateRequestFilter();
    var stateGetter = CreateStateGetter(filter, new Dictionary<string, DeviceState[]>
    {
      ["Device A"] = [CreateState(DateTime.UnixEpoch, ("Temperature", AresValueHelper.CreateNumber(1)))],
      ["Device B"] = [CreateState(DateTime.UnixEpoch.AddSeconds(1), ("Enabled", AresValueHelper.CreateBool(true)))]
    });

    var generator = new DeviceStateDatasetGenerator(stateGetter.Object);
    var datasets = await generator.GenerateAsync(filter);

    Assert.That(datasets.Select(dataset => dataset.Name), Is.EquivalentTo(["Device A", "Device B"]));
  }

  [Test]
  public async Task GenerateAsync_SortsRowsAndUsesTimestampValues()
  {
    var filter = new DeviceStateRequestFilter();
    var firstTimestamp = DateTime.UnixEpoch.AddSeconds(1);
    var secondTimestamp = DateTime.UnixEpoch.AddSeconds(2);
    var stateGetter = CreateStateGetter(filter, new Dictionary<string, DeviceState[]>
    {
      ["Device A"] =
      [
        CreateState(secondTimestamp, ("Temperature", AresValueHelper.CreateNumber(2))),
        CreateState(firstTimestamp, ("Temperature", AresValueHelper.CreateNumber(1)))
      ]
    });

    var generator = new DeviceStateDatasetGenerator(stateGetter.Object);
    var dataset = (await generator.GenerateAsync(filter)).Single();

    using(Assert.EnterMultipleScope())
    {
      Assert.That(dataset.Columns[0].Name, Is.EqualTo("Timestamp"));
      Assert.That(dataset.Columns[0].Schema.Type, Is.EqualTo(AresDataType.Timestamp));
      Assert.That(dataset.Rows[0].Data.Fields["Timestamp"].KindCase, Is.EqualTo(AresValue.KindOneofCase.TimestampValue));
      Assert.That(dataset.Rows[0].Data.Fields["Timestamp"].TimestampValue, Is.EqualTo(Timestamp.FromDateTime(firstTimestamp)));
      Assert.That(dataset.Rows[1].Data.Fields["Timestamp"].TimestampValue, Is.EqualTo(Timestamp.FromDateTime(secondTimestamp)));
    }
  }

  [Test]
  public async Task GenerateAsync_UsesUnionOfStateFieldsForTypedColumns()
  {
    var filter = new DeviceStateRequestFilter();
    var measuredAt = Timestamp.FromDateTime(DateTime.UnixEpoch.AddSeconds(10));
    var stateGetter = CreateStateGetter(filter, new Dictionary<string, DeviceState[]>
    {
      ["Device A"] =
      [
        CreateState(
          DateTime.UnixEpoch,
          ("Temperature", AresValueHelper.CreateNumber(12.3)),
          ("Name", AresValueHelper.CreateString("alpha")),
          ("Enabled", AresValueHelper.CreateBool(true))),
        CreateState(
          DateTime.UnixEpoch.AddSeconds(1),
          ("MeasuredAt", AresValueHelper.CreateTimestamp(measuredAt)),
          ("Mass", AresValueHelper.CreateQuantity(4.5, QuantityType.Mass, "g")))
      ]
    });

    var generator = new DeviceStateDatasetGenerator(stateGetter.Object);
    var dataset = (await generator.GenerateAsync(filter)).Single();

    using(Assert.EnterMultipleScope())
    {
      Assert.That(dataset.Columns.Select(column => column.Name), Is.EqualTo([
          "Timestamp",
      "Enabled",
      "Mass",
      "MeasuredAt",
      "Name",
      "Temperature"
        ]));
      Assert.That(ColumnSchema(dataset, "Enabled").Type, Is.EqualTo(AresDataType.Boolean));
      Assert.That(ColumnSchema(dataset, "Mass").Type, Is.EqualTo(AresDataType.Quantity));
      Assert.That(ColumnSchema(dataset, "MeasuredAt").Type, Is.EqualTo(AresDataType.Timestamp));
      Assert.That(ColumnSchema(dataset, "Name").Type, Is.EqualTo(AresDataType.String));
      Assert.That(ColumnSchema(dataset, "Temperature").Type, Is.EqualTo(AresDataType.Number));
      Assert.That(dataset.Columns.Skip(1).All(column => column.Schema.Optional), Is.True);
      Assert.That(dataset.Rows[0].Data.Fields.ContainsKey("MeasuredAt"), Is.False);
      Assert.That(dataset.Rows[1].Data.Fields.ContainsKey("Temperature"), Is.False);
    }
  }

  [Test]
  public async Task GenerateAsync_RenamesDynamicTimestampField()
  {
    var filter = new DeviceStateRequestFilter();
    var dynamicTimestamp = Timestamp.FromDateTime(DateTime.UnixEpoch.AddSeconds(10));
    var stateGetter = CreateStateGetter(filter, new Dictionary<string, DeviceState[]>
    {
      ["Device A"] =
      [
        CreateState(
          DateTime.UnixEpoch,
          ("Timestamp", AresValueHelper.CreateTimestamp(dynamicTimestamp)))
      ]
    });

    var generator = new DeviceStateDatasetGenerator(stateGetter.Object);
    var dataset = (await generator.GenerateAsync(filter)).Single();

    using(Assert.EnterMultipleScope())
    {
      Assert.That(dataset.Columns.Select(column => column.Name), Is.EqualTo(new[] { "Timestamp", "Data.Timestamp" }));
      Assert.That(dataset.Rows.Single().Data.Fields["Timestamp"].TimestampValue, Is.EqualTo(Timestamp.FromDateTime(DateTime.UnixEpoch)));
      Assert.That(dataset.Rows.Single().Data.Fields["Data.Timestamp"].TimestampValue, Is.EqualTo(dynamicTimestamp));
    }
  }

  [Test]
  public async Task GenerateAsync_ExpandsTopLevelStructFields()
  {
    var filter = new DeviceStateRequestFilter();
    var position = CreateStruct(
      ("X", AresValueHelper.CreateNumber(1.2)),
      ("Y", AresValueHelper.CreateNumber(3.4)));
    var stateGetter = CreateStateGetter(filter, new Dictionary<string, DeviceState[]>
    {
      ["Device A"] = [CreateState(DateTime.UnixEpoch, ("Position", AresValueHelper.CreateStruct(position)))]
    });

    var generator = new DeviceStateDatasetGenerator(stateGetter.Object);
    var dataset = (await generator.GenerateAsync(filter)).Single();
    var row = dataset.Rows.Single();

    using(Assert.EnterMultipleScope())
    {
      Assert.That(dataset.Columns.Select(column => column.Name), Is.EqualTo([
        "Timestamp",
        "Position.X",
        "Position.Y"
      ]));
      Assert.That(dataset.Columns.Any(column => column.Name == "Position"), Is.False);
      Assert.That(ColumnSchema(dataset, "Position.X").Type, Is.EqualTo(AresDataType.Number));
      Assert.That(row.Data.Fields["Position.X"].NumberValue, Is.EqualTo(1.2));
      Assert.That(row.Data.Fields["Position.Y"].NumberValue, Is.EqualTo(3.4));
      Assert.That(row.Data.Fields.ContainsKey("Position"), Is.False);
    }
  }

  [Test]
  public async Task GenerateAsync_ExpandsNestedStructFieldsRecursively()
  {
    var filter = new DeviceStateRequestFilter();
    var offset = CreateStruct(("X", AresValueHelper.CreateNumber(5.6)));
    var position = CreateStruct(
      ("Offset", AresValueHelper.CreateStruct(offset)),
      ("Y", AresValueHelper.CreateNumber(7.8)));
    var stateGetter = CreateStateGetter(filter, new Dictionary<string, DeviceState[]>
    {
      ["Device A"] = [CreateState(DateTime.UnixEpoch, ("Position", AresValueHelper.CreateStruct(position)))]
    });

    var generator = new DeviceStateDatasetGenerator(stateGetter.Object);
    var dataset = (await generator.GenerateAsync(filter)).Single();
    var row = dataset.Rows.Single();

    using(Assert.EnterMultipleScope())
    {
      Assert.That(dataset.Columns.Select(column => column.Name), Is.EqualTo([
        "Timestamp",
        "Position.Offset.X",
        "Position.Y"
      ]));
      Assert.That(dataset.Columns.Any(column => column.Name is "Position" or "Position.Offset"), Is.False);
      Assert.That(row.Data.Fields["Position.Offset.X"].NumberValue, Is.EqualTo(5.6));
      Assert.That(row.Data.Fields["Position.Y"].NumberValue, Is.EqualTo(7.8));
      Assert.That(row.Data.Fields.ContainsKey("Position.Offset"), Is.False);
    }
  }

  [Test]
  public async Task GenerateAsync_ClonesExpandedStructFieldValues()
  {
    var filter = new DeviceStateRequestFilter();
    var original = AresValueHelper.CreateString("before");
    var metadata = CreateStruct(("Name", original));
    var stateGetter = CreateStateGetter(filter, new Dictionary<string, DeviceState[]>
    {
      ["Device A"] = [CreateState(DateTime.UnixEpoch, ("Metadata", AresValueHelper.CreateStruct(metadata)))]
    });

    var generator = new DeviceStateDatasetGenerator(stateGetter.Object);
    var dataset = (await generator.GenerateAsync(filter)).Single();

    original.StringValue = "after";

    Assert.That(dataset.Rows.Single().Data.Fields["Metadata.Name"].StringValue, Is.EqualTo("before"));
  }

  [Test]
  public async Task GenerateAsync_ClonesSourceStateValues()
  {
    var filter = new DeviceStateRequestFilter();
    var original = AresValueHelper.CreateString("before");
    var stateGetter = CreateStateGetter(filter, new Dictionary<string, DeviceState[]>
    {
      ["Device A"] = [CreateState(DateTime.UnixEpoch, ("Name", original))]
    });

    var generator = new DeviceStateDatasetGenerator(stateGetter.Object);
    var dataset = (await generator.GenerateAsync(filter)).Single();

    original.StringValue = "after";

    Assert.That(dataset.Rows.Single().Data.Fields["Name"].StringValue, Is.EqualTo("before"));
  }

  [Test]
  public async Task GenerateAsync_WithIntervalCreatesFixedTimestampRows()
  {
    var filter = new DeviceStateRequestFilter
    {
      Start = Timestamp.FromDateTime(DateTime.UnixEpoch),
      End = Timestamp.FromDateTime(DateTime.UnixEpoch.AddSeconds(4)),
      Interval = Duration.FromTimeSpan(TimeSpan.FromSeconds(2))
    };
    var stateGetter = CreateStateGetter(filter, new Dictionary<string, DeviceState[]>
    {
      ["Device A"] =
      [
        CreateState(DateTime.UnixEpoch, ("Temperature", AresValueHelper.CreateNumber(1))),
        CreateState(DateTime.UnixEpoch.AddSeconds(3), ("Temperature", AresValueHelper.CreateNumber(2)))
      ]
    });

    var generator = new DeviceStateDatasetGenerator(stateGetter.Object);
    var dataset = (await generator.GenerateAsync(filter)).Single();

    Assert.That(dataset.Rows.Select(row => row.Data.Fields["Timestamp"].TimestampValue), Is.EqualTo([
      Timestamp.FromDateTime(DateTime.UnixEpoch),
      Timestamp.FromDateTime(DateTime.UnixEpoch.AddSeconds(2)),
      Timestamp.FromDateTime(DateTime.UnixEpoch.AddSeconds(4))
    ]));
  }

  [Test]
  public async Task GenerateAsync_WithIntervalUsesLatestStateAtOrBeforeTimestamp()
  {
    var filter = new DeviceStateRequestFilter
    {
      Start = Timestamp.FromDateTime(DateTime.UnixEpoch),
      End = Timestamp.FromDateTime(DateTime.UnixEpoch.AddSeconds(4)),
      Interval = Duration.FromTimeSpan(TimeSpan.FromSeconds(2))
    };
    var stateGetter = CreateStateGetter(filter, new Dictionary<string, DeviceState[]>
    {
      ["Device A"] =
      [
        CreateState(DateTime.UnixEpoch, ("Temperature", AresValueHelper.CreateNumber(1))),
        CreateState(DateTime.UnixEpoch.AddSeconds(3), ("Temperature", AresValueHelper.CreateNumber(2)))
      ]
    });

    var generator = new DeviceStateDatasetGenerator(stateGetter.Object);
    var dataset = (await generator.GenerateAsync(filter)).Single();

    Assert.That(dataset.Rows.Select(row => row.Data.Fields["Temperature"].NumberValue), Is.EqualTo([1, 1, 2]));
  }

  [Test]
  public async Task GenerateAsync_WithEmptyDeviceStateCollectionReturnsDatasetWithNoRows()
  {
    var filter = new DeviceStateRequestFilter();
    var stateGetter = CreateStateGetter(filter, new Dictionary<string, DeviceState[]>
    {
      ["Device A"] = []
    });

    var generator = new DeviceStateDatasetGenerator(stateGetter.Object);
    var dataset = (await generator.GenerateAsync(filter)).Single();

    using(Assert.EnterMultipleScope())
    {
      Assert.That(dataset.Name, Is.EqualTo("Device A"));
      Assert.That(dataset.Columns.Select(column => column.Name), Is.EqualTo(["Timestamp"]));
      Assert.That(dataset.Rows, Is.Empty);
    }
  }

  [Test]
  public void GenerateAsync_ThrowsWhenAlreadyCanceled()
  {
    var filter = new DeviceStateRequestFilter();
    var stateGetter = new Mock<IDeviceStateGetter>();
    using var cancellationTokenSource = new CancellationTokenSource();
    cancellationTokenSource.Cancel();

    var generator = new DeviceStateDatasetGenerator(stateGetter.Object);

    Assert.ThrowsAsync<OperationCanceledException>(async () => await generator.GenerateAsync(filter, cancellationTokenSource.Token));
    stateGetter.Verify(getter => getter.GetStates<DeviceState>(It.IsAny<DeviceStateRequestFilter>()), Times.Never);
  }

  private static AresValueSchema ColumnSchema(AresDataset dataset, string columnName)
  {
    return dataset.Columns.Single(column => column.Name == columnName).Schema;
  }

  private static Mock<IDeviceStateGetter> CreateStateGetter(
    DeviceStateRequestFilter filter,
    IDictionary<string, DeviceState[]> states)
  {
    var stateGetter = new Mock<IDeviceStateGetter>();
    stateGetter
      .Setup(getter => getter.GetStates<DeviceState>(filter))
      .ReturnsAsync(states);
    return stateGetter;
  }

  private static DeviceState CreateState(DateTime timestamp, params (string Name, AresValue Value)[] values)
  {
    var state = new DeviceState
    {
      Data = new AresStruct(),
      Timestamp = Timestamp.FromDateTime(timestamp)
    };

    foreach(var value in values)
    {
      state.Data.Fields[value.Name] = value.Value;
    }

    return state;
  }

  private static AresStruct CreateStruct(params (string Name, AresValue Value)[] values)
  {
    var aresStruct = new AresStruct();
    foreach(var value in values)
    {
      aresStruct.Fields[value.Name] = value.Value;
    }

    return aresStruct;
  }
}
