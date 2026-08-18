using Ares.Core.DataManagement.DataMappers;
using Ares.Core.Device.State.Export.StateGetters;
using Ares.Datamodel;
using Ares.Datamodel.Device;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Templates;
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;
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

    var generator = CreateGenerator(stateGetter.Object);
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

    var generator = CreateGenerator(stateGetter.Object);
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

    var generator = CreateGenerator(stateGetter.Object);
    var dataset = (await generator.GenerateAsync(filter)).Single();

    using(Assert.EnterMultipleScope())
    {
      Assert.That(dataset.Columns.Select(column => column.Name), Is.EqualTo([
        "Timestamp",
        "Campaign",
        "Experiment Number",
        "Step Name",
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

    var generator = CreateGenerator(stateGetter.Object);
    var dataset = (await generator.GenerateAsync(filter)).Single();

    using(Assert.EnterMultipleScope())
    {
      Assert.That(dataset.Columns.Select(column => column.Name), Is.EqualTo([
        "Timestamp",
        "Campaign",
        "Experiment Number",
        "Step Name",
        "Data.Timestamp"
      ]));
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

    var generator = CreateGenerator(stateGetter.Object);
    var dataset = (await generator.GenerateAsync(filter)).Single();
    var row = dataset.Rows.Single();

    using(Assert.EnterMultipleScope())
    {
      Assert.That(dataset.Columns.Select(column => column.Name), Is.EqualTo([
        "Timestamp",
        "Campaign",
        "Experiment Number",
        "Step Name",
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

    var generator = CreateGenerator(stateGetter.Object);
    var dataset = (await generator.GenerateAsync(filter)).Single();
    var row = dataset.Rows.Single();

    using(Assert.EnterMultipleScope())
    {
      Assert.That(dataset.Columns.Select(column => column.Name), Is.EqualTo([
        "Timestamp",
        "Campaign",
        "Experiment Number",
        "Step Name",
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

    var generator = CreateGenerator(stateGetter.Object);
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

    var generator = CreateGenerator(stateGetter.Object);
    var dataset = (await generator.GenerateAsync(filter)).Single();

    original.StringValue = "after";

    Assert.That(dataset.Rows.Single().Data.Fields["Name"].StringValue, Is.EqualTo("before"));
  }

  [Test]
  public async Task GenerateAsync_WithMinimumSampleIntervalDownsamplesRowsByElapsedTime()
  {
    var filter = new DeviceStateRequestFilter
    {
      Start = Timestamp.FromDateTime(DateTime.UnixEpoch),
      End = Timestamp.FromDateTime(DateTime.UnixEpoch.AddSeconds(5)),
      Interval = Duration.FromTimeSpan(TimeSpan.FromSeconds(2))
    };
    var stateGetter = CreateStateGetter(filter, new Dictionary<string, DeviceState[]>
    {
      ["Device A"] =
      [
        CreateState(DateTime.UnixEpoch, ("Temperature", AresValueHelper.CreateNumber(1))),
        CreateState(DateTime.UnixEpoch.AddSeconds(1), ("Temperature", AresValueHelper.CreateNumber(2))),
        CreateState(DateTime.UnixEpoch.AddSeconds(3), ("Temperature", AresValueHelper.CreateNumber(3))),
        CreateState(DateTime.UnixEpoch.AddSeconds(4), ("Temperature", AresValueHelper.CreateNumber(4))),
        CreateState(DateTime.UnixEpoch.AddSeconds(5), ("Temperature", AresValueHelper.CreateNumber(5)))
      ]
    });

    var generator = CreateGenerator(stateGetter.Object);
    var dataset = (await generator.GenerateAsync(filter)).Single();

    Assert.That(dataset.Rows.Select(row => row.Data.Fields["Timestamp"].TimestampValue), Is.EqualTo([
      Timestamp.FromDateTime(DateTime.UnixEpoch),
      Timestamp.FromDateTime(DateTime.UnixEpoch.AddSeconds(3)),
      Timestamp.FromDateTime(DateTime.UnixEpoch.AddSeconds(5))
    ]));
  }

  [Test]
  public async Task GenerateAsync_WithMinimumSampleIntervalUsesRetrievedRowValues()
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
        CreateState(DateTime.UnixEpoch.AddSeconds(1), ("Temperature", AresValueHelper.CreateNumber(2))),
        CreateState(DateTime.UnixEpoch.AddSeconds(3), ("Temperature", AresValueHelper.CreateNumber(3)))
      ]
    });

    var generator = CreateGenerator(stateGetter.Object);
    var dataset = (await generator.GenerateAsync(filter)).Single();

    Assert.That(dataset.Rows.Select(row => row.Data.Fields["Temperature"].NumberValue), Is.EqualTo([1, 3]));
  }

  [Test]
  public async Task GenerateAsync_WithEmptyDeviceStateCollectionReturnsDatasetWithNoRows()
  {
    var filter = new DeviceStateRequestFilter();
    var stateGetter = CreateStateGetter(filter, new Dictionary<string, DeviceState[]>
    {
      ["Device A"] = []
    });

    var generator = CreateGenerator(stateGetter.Object);
    var dataset = (await generator.GenerateAsync(filter)).Single();

    using(Assert.EnterMultipleScope())
    {
      Assert.That(dataset.Name, Is.EqualTo("Device A"));
      Assert.That(dataset.Columns.Select(column => column.Name), Is.EqualTo(["Timestamp", "Campaign", "Experiment Number", "Step Name"]));
      Assert.That(dataset.Rows, Is.Empty);
    }
  }

  [Test]
  public async Task GenerateAsync_AddsCampaignExperimentAndStepInformation()
  {
    var timestamp = DateTime.UnixEpoch.AddSeconds(5);
    var filter = new DeviceStateRequestFilter
    {
      Start = Timestamp.FromDateTime(DateTime.UnixEpoch.AddSeconds(4)),
      End = Timestamp.FromDateTime(DateTime.UnixEpoch.AddSeconds(6))
    };
    var stateGetter = CreateStateGetter(filter, new Dictionary<string, DeviceState[]>
    {
      ["Device A"] = [CreateState(timestamp, ("Temperature", AresValueHelper.CreateNumber(1)))]
    });
    var firstExperiment = CreateExperiment(
      DateTime.UnixEpoch.AddSeconds(1),
      DateTime.UnixEpoch.AddSeconds(3));
    var secondExperiment = CreateExperiment(
      DateTime.UnixEpoch.AddSeconds(4),
      DateTime.UnixEpoch.AddSeconds(8),
      CreateStep(
        Guid.NewGuid().ToString(),
        DateTime.UnixEpoch.AddSeconds(4),
        DateTime.UnixEpoch.AddSeconds(8)));
    secondExperiment.ExperimentOverview.Template.StepTemplates.Add(new StepTemplate
    {
      UniqueId = Guid.NewGuid().ToString(),
      Name = "Heat"
    });
    var summary = CreateCampaign(
      "Campaign A",
      DateTime.UnixEpoch,
      DateTime.UnixEpoch.AddSeconds(10),
      firstExperiment,
      secondExperiment);

    var dataset = (await CreateGenerator(stateGetter.Object, summary).GenerateAsync(filter)).Single();
    var row = dataset.Rows.Single();

    using(Assert.EnterMultipleScope())
    {
      Assert.That(row.Data.Fields["Campaign"].StringValue, Is.EqualTo("Campaign A"));
      Assert.That(row.Data.Fields["Experiment Number"].IntValue, Is.EqualTo(2));
      Assert.That(row.Data.Fields["Step Name"].StringValue, Is.EqualTo("Heat"));
      Assert.That(ColumnSchema(dataset, "Campaign").Optional, Is.True);
      Assert.That(ColumnSchema(dataset, "Experiment Number").Optional, Is.True);
      Assert.That(ColumnSchema(dataset, "Step Name").Optional, Is.True);
    }
  }

  [Test]
  public async Task GenerateAsync_LeavesExecutionInformationBlankWhenNoCampaignIsActive()
  {
    var filter = new DeviceStateRequestFilter();
    var stateGetter = CreateStateGetter(filter, new Dictionary<string, DeviceState[]>
    {
      ["Device A"] = [CreateState(DateTime.UnixEpoch.AddSeconds(20))]
    });
    var summary = CreateCampaign(
      "Campaign A",
      DateTime.UnixEpoch,
      DateTime.UnixEpoch.AddSeconds(10));

    var row = (await CreateGenerator(stateGetter.Object, summary).GenerateAsync(filter)).Single().Rows.Single();

    using(Assert.EnterMultipleScope())
    {
      Assert.That(row.Data.Fields.ContainsKey("Campaign"), Is.False);
      Assert.That(row.Data.Fields.ContainsKey("Experiment Number"), Is.False);
      Assert.That(row.Data.Fields.ContainsKey("Step Name"), Is.False);
    }
  }

  [Test]
  public async Task GenerateAsync_DuplicatesRowsForConcurrentCampaigns()
  {
    var filter = new DeviceStateRequestFilter();
    var stateGetter = CreateStateGetter(filter, new Dictionary<string, DeviceState[]>
    {
      ["Device A"] = [CreateState(DateTime.UnixEpoch.AddSeconds(5))]
    });
    var firstCampaign = CreateCampaign(
      "Campaign A",
      DateTime.UnixEpoch,
      DateTime.UnixEpoch.AddSeconds(10));
    var secondCampaign = CreateCampaign(
      "Campaign B",
      DateTime.UnixEpoch.AddSeconds(5),
      DateTime.UnixEpoch.AddSeconds(15));

    var rows = (await CreateGenerator(stateGetter.Object, firstCampaign, secondCampaign).GenerateAsync(filter)).Single().Rows;

    Assert.That(rows.Select(row => row.Data.Fields["Campaign"].StringValue), Is.EqualTo(["Campaign A", "Campaign B"]));
  }

  [Test]
  public async Task GenerateAsync_WithCompletedCampaignFilterOnlyRetrievesMatchingCampaign()
  {
    var firstCampaign = CreateCampaign(
      "Campaign A",
      DateTime.UnixEpoch,
      DateTime.UnixEpoch.AddSeconds(10));
    var secondCampaign = CreateCampaign(
      "Campaign B",
      DateTime.UnixEpoch,
      DateTime.UnixEpoch.AddSeconds(10));
    var filter = new DeviceStateRequestFilter
    {
      CompletedCampaignId = firstCampaign.UniqueId
    };
    var stateGetter = CreateStateGetter(filter, new Dictionary<string, DeviceState[]>
    {
      ["Device A"] = [CreateState(DateTime.UnixEpoch.AddSeconds(5))]
    });

    var rows = (await CreateGenerator(stateGetter.Object, firstCampaign, secondCampaign).GenerateAsync(filter)).Single().Rows;

    Assert.That(rows.Select(row => row.Data.Fields["Campaign"].StringValue), Is.EqualTo(["Campaign A"]));
  }

  [Test]
  public async Task GenerateAsync_PreservesReservedDeviceFieldsUnderDataPrefix()
  {
    var filter = new DeviceStateRequestFilter();
    var stateGetter = CreateStateGetter(filter, new Dictionary<string, DeviceState[]>
    {
      ["Device A"] =
      [
        CreateState(
          DateTime.UnixEpoch,
          ("Campaign", AresValueHelper.CreateString("device value")),
          ("Experiment Number", AresValueHelper.CreateInt(42)),
          ("Step Name", AresValueHelper.CreateString("device step")))
      ]
    });

    var dataset = (await CreateGenerator(stateGetter.Object).GenerateAsync(filter)).Single();
    var row = dataset.Rows.Single();

    using(Assert.EnterMultipleScope())
    {
      Assert.That(row.Data.Fields["Data.Campaign"].StringValue, Is.EqualTo("device value"));
      Assert.That(row.Data.Fields["Data.Experiment Number"].IntValue, Is.EqualTo(42));
      Assert.That(row.Data.Fields["Data.Step Name"].StringValue, Is.EqualTo("device step"));
    }
  }

  [Test]
  public void GenerateAsync_ThrowsWhenAlreadyCanceled()
  {
    var filter = new DeviceStateRequestFilter();
    var stateGetter = new Mock<IDeviceStateGetter>();
    using var cancellationTokenSource = new CancellationTokenSource();
    cancellationTokenSource.Cancel();

    var generator = CreateGenerator(stateGetter.Object);

    Assert.ThrowsAsync<OperationCanceledException>(async () => await generator.GenerateAsync(filter, cancellationTokenSource.Token));
    stateGetter.Verify(getter => getter.GetStates<DeviceState>(It.IsAny<DeviceStateRequestFilter>()), Times.Never);
  }

  [Test]
  public void GenerateAsync_ThrowsWhenCanceledAfterFetchingStates()
  {
    var filter = new DeviceStateRequestFilter();
    var states = new Dictionary<string, DeviceState[]>
    {
      ["Device A"] = [CreateState(DateTime.UnixEpoch, ("Temperature", AresValueHelper.CreateNumber(1)))]
    };
    var stateGetter = new Mock<IDeviceStateGetter>();
    using var cancellationTokenSource = new CancellationTokenSource();
    stateGetter
      .Setup(getter => getter.GetStates<DeviceState>(filter, cancellationTokenSource.Token))
      .Returns(() =>
      {
        cancellationTokenSource.Cancel();
        return Task.FromResult<IDictionary<string, DeviceState[]>>(states);
      });

    var generator = CreateGenerator(stateGetter.Object);

    Assert.ThrowsAsync<OperationCanceledException>(async () => await generator.GenerateAsync(filter, cancellationTokenSource.Token));
  }

  [Test]
  public void GenerateAsync_WithMinimumSampleIntervalThrowsWhenCanceledDuringRowGeneration()
  {
    var filter = new DeviceStateRequestFilter
    {
      Start = Timestamp.FromDateTime(DateTime.UnixEpoch),
      End = Timestamp.FromDateTime(DateTime.UnixEpoch.AddDays(1)),
      Interval = Duration.FromTimeSpan(TimeSpan.FromMilliseconds(1))
    };
    var stateGetter = CreateStateGetter(filter, new Dictionary<string, DeviceState[]>
    {
      ["Device A"] = Enumerable
        .Range(0, 100_000)
        .Select(index => CreateState(DateTime.UnixEpoch.AddMilliseconds(index), ("Temperature", AresValueHelper.CreateNumber(index))))
        .ToArray()
    });
    using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(1));

    var generator = CreateGenerator(stateGetter.Object);

    Assert.ThrowsAsync<OperationCanceledException>(async () => await generator.GenerateAsync(filter, cancellationTokenSource.Token));
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
    stateGetter
      .Setup(getter => getter.GetStates<DeviceState>(filter, It.IsAny<CancellationToken>()))
      .ReturnsAsync(states);
    return stateGetter;
  }

  private static DeviceStateDatasetGenerator CreateGenerator(
    IDeviceStateGetter stateGetter,
    params CampaignExecutionSummary[] summaries)
  {
    var options = new DbContextOptionsBuilder<CoreDatabaseContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;
    using(var context = new CoreDatabaseContext(options))
    {
      context.CampaignExecutionSummaries.AddRange(summaries);
      context.SaveChanges();
    }

    var contextFactory = new Mock<IDbContextFactory<CoreDatabaseContext>>();
    contextFactory
      .Setup(factory => factory.CreateDbContextAsync(It.IsAny<CancellationToken>()))
      .ReturnsAsync(() => new CoreDatabaseContext(options));
    return new DeviceStateDatasetGenerator(stateGetter, contextFactory.Object);
  }

  private static CampaignExecutionSummary CreateCampaign(
    string name,
    DateTime timeStarted,
    DateTime timeFinished,
    params ExperimentExecutionSummary[] experiments)
  {
    var summary = new CampaignExecutionSummary
    {
      UniqueId = Guid.NewGuid().ToString(),
      CampaignId = Guid.NewGuid().ToString(),
      CampaignName = name,
      ExecutionInfo = CreateExecutionInfo(timeStarted, timeFinished)
    };
    summary.ExperimentSummaries.AddRange(experiments);
    return summary;
  }

  private static ExperimentExecutionSummary CreateExperiment(
    DateTime timeStarted,
    DateTime timeFinished,
    params StepExecutionSummary[] steps)
  {
    var experiment = new ExperimentExecutionSummary
    {
      UniqueId = Guid.NewGuid().ToString(),
      ExperimentId = Guid.NewGuid().ToString(),
      ExecutionInfo = CreateExecutionInfo(timeStarted, timeFinished),
      ExperimentOverview = new ExperimentOverview
      {
        UniqueId = Guid.NewGuid().ToString(),
        Template = new ExperimentTemplate
        {
          UniqueId = Guid.NewGuid().ToString(),
          Name = "Experiment"
        }
      }
    };
    experiment.StepSummaries.AddRange(steps);
    return experiment;
  }

  private static StepExecutionSummary CreateStep(string stepId, DateTime timeStarted, DateTime timeFinished)
  {
    return new StepExecutionSummary
    {
      UniqueId = Guid.NewGuid().ToString(),
      StepId = stepId,
      ExecutionInfo = CreateExecutionInfo(timeStarted, timeFinished)
    };
  }

  private static ExecutionInfo CreateExecutionInfo(DateTime timeStarted, DateTime timeFinished)
  {
    return new ExecutionInfo
    {
      UniqueId = Guid.NewGuid().ToString(),
      TimeStarted = Timestamp.FromDateTime(timeStarted),
      TimeFinished = Timestamp.FromDateTime(timeFinished)
    };
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
