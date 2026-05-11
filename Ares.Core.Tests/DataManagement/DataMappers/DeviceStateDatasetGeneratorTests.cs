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
    var stateGetter = CreateStateGetter(filter, new Dictionary<string, IEnumerable<DeviceState>>
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
    var stateGetter = CreateStateGetter(filter, new Dictionary<string, IEnumerable<DeviceState>>
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
    var stateGetter = CreateStateGetter(filter, new Dictionary<string, IEnumerable<DeviceState>>
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
    var stateGetter = CreateStateGetter(filter, new Dictionary<string, IEnumerable<DeviceState>>
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
  public async Task GenerateAsync_ClonesSourceStateValues()
  {
    var filter = new DeviceStateRequestFilter();
    var original = AresValueHelper.CreateString("before");
    var stateGetter = CreateStateGetter(filter, new Dictionary<string, IEnumerable<DeviceState>>
    {
      ["Device A"] = [CreateState(DateTime.UnixEpoch, ("Name", original))]
    });

    var generator = new DeviceStateDatasetGenerator(stateGetter.Object);
    var dataset = (await generator.GenerateAsync(filter)).Single();

    original.StringValue = "after";

    Assert.That(dataset.Rows.Single().Data.Fields["Name"].StringValue, Is.EqualTo("before"));
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
    IDictionary<string, IEnumerable<DeviceState>> states)
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
}
