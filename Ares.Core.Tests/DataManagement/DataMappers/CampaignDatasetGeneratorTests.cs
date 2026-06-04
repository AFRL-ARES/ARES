using Ares.Core.DataManagement.DataMappers;
using Ares.Datamodel;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Templates;
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Ares.Core.Tests.DataManagement.DataMappers;

internal class CampaignDatasetGeneratorTests
{
  [Test]
  public async Task GenerateAsync_ReturnsEmptyArray_WhenSummaryDoesNotExist()
  {
    var generator = new CampaignDatasetGenerator(CreateContextFactory().Object);

    var datasets = await generator.GenerateAsync("missing-summary");

    Assert.That(datasets, Is.Empty);
  }

  [Test]
  public async Task GenerateAsync_SortsExperimentRowsAndCreatesFixedColumns()
  {
    var summaryId = Guid.NewGuid().ToString();
    var firstStart = DateTime.UnixEpoch.AddSeconds(1);
    var firstEnd = DateTime.UnixEpoch.AddSeconds(2);
    var secondStart = DateTime.UnixEpoch.AddSeconds(3);
    var secondEnd = DateTime.UnixEpoch.AddSeconds(4);
    var summary = CreateCampaignSummary(
      summaryId,
      "Campaign A",
      CreateExperiment(secondStart, secondEnd, analysisResult: 2.5),
      CreateExperiment(firstStart, firstEnd, analysisResult: 1.5));
    var generator = CreateGenerator(summary);

    var dataset = (await generator.GenerateAsync(summaryId)).Single();

    using(Assert.EnterMultipleScope())
    {
      Assert.That(dataset.Name, Is.EqualTo("Experiments"));
      Assert.That(dataset.Columns.Take(9).Select(column => column.Name), Is.EqualTo([
        "Experiment Number",
        "Experiment Execution ID",
        "Experiment ID",
        "Experiment Template",
        "Time Started",
        "Time Finished",
        "Duration Seconds",
        "Analysis Result",
        "Result Output Path"
      ]));
      Assert.That(ColumnSchema(dataset, "Experiment Number").Type, Is.EqualTo(AresDataType.Int));
      Assert.That(ColumnSchema(dataset, "Time Started").Type, Is.EqualTo(AresDataType.Timestamp));
      Assert.That(ColumnSchema(dataset, "Time Finished").Type, Is.EqualTo(AresDataType.Timestamp));
      Assert.That(ColumnSchema(dataset, "Duration Seconds").Type, Is.EqualTo(AresDataType.Number));
      Assert.That(ColumnSchema(dataset, "Analysis Result").Type, Is.EqualTo(AresDataType.Number));
      Assert.That(ColumnSchema(dataset, "Analysis Result").Optional, Is.True);
      Assert.That(dataset.Rows[0].Data.Fields["Experiment Number"].IntValue, Is.EqualTo(1));
      Assert.That(dataset.Rows[0].Data.Fields["Time Started"].TimestampValue, Is.EqualTo(Timestamp.FromDateTime(firstStart)));
      Assert.That(dataset.Rows[0].Data.Fields["Time Finished"].TimestampValue, Is.EqualTo(Timestamp.FromDateTime(firstEnd)));
      Assert.That(dataset.Rows[0].Data.Fields["Duration Seconds"].NumberValue, Is.EqualTo(1));
      Assert.That(dataset.Rows[0].Data.Fields["Analysis Result"].NumberValue, Is.EqualTo(1.5));
      Assert.That(dataset.Rows[1].Data.Fields["Experiment Number"].IntValue, Is.EqualTo(2));
      Assert.That(dataset.Rows[1].Data.Fields["Time Started"].TimestampValue, Is.EqualTo(Timestamp.FromDateTime(secondStart)));
      Assert.That(dataset.Rows[1].Data.Fields["Time Finished"].TimestampValue, Is.EqualTo(Timestamp.FromDateTime(secondEnd)));
      Assert.That(dataset.Rows[1].Data.Fields["Duration Seconds"].NumberValue, Is.EqualTo(1));
      Assert.That(dataset.Rows[1].Data.Fields["Analysis Result"].NumberValue, Is.EqualTo(2.5));
    }
  }

  [Test]
  public async Task GenerateAsync_UsesResultFieldsForTypedOptionalColumns()
  {
    var summaryId = Guid.NewGuid().ToString();
    var original = AresValueHelper.CreateString("before");
    var summary = CreateCampaignSummary(
      summaryId,
      "Campaign A",
      CreateExperiment(DateTime.UnixEpoch, DateTime.UnixEpoch.AddSeconds(1), resultFields: [
        ("Yield", AresValueHelper.CreateNumber(12.3)),
        ("Comment", original)
      ]),
      CreateExperiment(DateTime.UnixEpoch.AddSeconds(2), DateTime.UnixEpoch.AddSeconds(3), resultFields: [
        ("Mass", AresValueHelper.CreateQuantity(4.5, QuantityType.Mass, "g"))
      ]));
    var generator = CreateGenerator(summary);

    var dataset = (await generator.GenerateAsync(summaryId)).Single();
    original.StringValue = "after";

    using(Assert.EnterMultipleScope())
    {
      Assert.That(ColumnSchema(dataset, "Output.Yield").Type, Is.EqualTo(AresDataType.Number));
      Assert.That(ColumnSchema(dataset, "Output.Yield").Optional, Is.True);
      Assert.That(ColumnSchema(dataset, "Output.Comment").Type, Is.EqualTo(AresDataType.String));
      Assert.That(ColumnSchema(dataset, "Output.Comment").Optional, Is.True);
      Assert.That(ColumnSchema(dataset, "Output.Mass").Type, Is.EqualTo(AresDataType.Quantity));
      Assert.That(ColumnSchema(dataset, "Output.Mass").Optional, Is.True);
      Assert.That(dataset.Rows[0].Data.Fields["Output.Yield"].NumberValue, Is.EqualTo(12.3));
      Assert.That(dataset.Rows[0].Data.Fields["Output.Comment"].StringValue, Is.EqualTo("before"));
      Assert.That(dataset.Rows[0].Data.Fields.ContainsKey("Output.Mass"), Is.False);
      Assert.That(dataset.Rows[1].Data.Fields["Output.Mass"].QuantityValue.Scalar, Is.EqualTo(4.5));
    }
  }

  [Test]
  public async Task GenerateAsync_IncludesExperimentIdentityTemplateAndOutputPath()
  {
    var summaryId = Guid.NewGuid().ToString();
    var experiment = CreateExperiment(DateTime.UnixEpoch, DateTime.UnixEpoch.AddSeconds(1));
    experiment.ExperimentId = "experiment-id";
    experiment.ExperimentOverview.Template = new ExperimentTemplate { Name = "Template A" };
    experiment.ResultOutputPath = "results/experiment.json";
    var generator = CreateGenerator(CreateCampaignSummary(summaryId, "Campaign A", experiment));

    var row = (await generator.GenerateAsync(summaryId)).Single().Rows.Single();

    using(Assert.EnterMultipleScope())
    {
      Assert.That(row.Data.Fields["Experiment Execution ID"].StringValue, Is.EqualTo(experiment.UniqueId));
      Assert.That(row.Data.Fields["Experiment ID"].StringValue, Is.EqualTo("experiment-id"));
      Assert.That(row.Data.Fields["Experiment Template"].StringValue, Is.EqualTo("Template A"));
      Assert.That(row.Data.Fields["Result Output Path"].StringValue, Is.EqualTo("results/experiment.json"));
    }
  }

  [Test]
  public async Task GenerateAsync_RecursivelyFlattensStructParametersAndResults()
  {
    var summaryId = Guid.NewGuid().ToString();
    var result = AresValueHelper.CreateStruct();
    result.StructValue.Fields["Nested"] = AresValueHelper.CreateStruct();
    result.StructValue.Fields["Nested"].StructValue.Fields["Value"] = AresValueHelper.CreateNumber(12.3);
    var parameter = AresValueHelper.CreateStruct();
    parameter.StructValue.Fields["Temp1"] = AresValueHelper.CreateNumber(22.5);
    var generator = CreateGenerator(CreateCampaignSummary(
      summaryId,
      "Campaign A",
      CreateExperiment(
        DateTime.UnixEpoch,
        DateTime.UnixEpoch.AddSeconds(1),
        resultFields: [("Metrics", result)],
        parameters: [CreateParameter("Temperatures", parameter)])));

    var dataset = (await generator.GenerateAsync(summaryId)).Single();

    using(Assert.EnterMultipleScope())
    {
      Assert.That(dataset.Columns.Select(column => column.Name), Does.Contain("Output.Metrics.Nested.Value"));
      Assert.That(dataset.Columns.Select(column => column.Name), Does.Contain("Input.Temperatures.Temp1"));
      Assert.That(dataset.Rows.Single().Data.Fields["Output.Metrics.Nested.Value"].NumberValue, Is.EqualTo(12.3));
      Assert.That(dataset.Rows.Single().Data.Fields["Input.Temperatures.Temp1"].NumberValue, Is.EqualTo(22.5));
    }
  }

  [Test]
  public async Task GenerateAsync_UsesAnySchemaForConflictingDynamicTypes()
  {
    var summaryId = Guid.NewGuid().ToString();
    var generator = CreateGenerator(CreateCampaignSummary(
      summaryId,
      "Campaign A",
      CreateExperiment(DateTime.UnixEpoch, DateTime.UnixEpoch.AddSeconds(1), resultFields: [("Value", AresValueHelper.CreateNumber(1))]),
      CreateExperiment(DateTime.UnixEpoch.AddSeconds(2), DateTime.UnixEpoch.AddSeconds(3), resultFields: [("Value", AresValueHelper.CreateString("one"))])));

    var dataset = (await generator.GenerateAsync(summaryId)).Single();

    Assert.That(ColumnSchema(dataset, "Output.Value").Type, Is.EqualTo(AresDataType.Any));
  }

  [Test]
  public async Task GenerateAsync_ExcludesStartupAndCloseoutSummaries()
  {
    var summaryId = Guid.NewGuid().ToString();
    var summary = CreateCampaignSummary(
      summaryId,
      "Campaign A",
      CreateExperiment(DateTime.UnixEpoch, DateTime.UnixEpoch.AddSeconds(1)));
    summary.StartupExecutionSummary = CreateExperiment(DateTime.UnixEpoch, DateTime.UnixEpoch.AddSeconds(1));
    summary.CloseoutExecutionSummary = CreateExperiment(DateTime.UnixEpoch, DateTime.UnixEpoch.AddSeconds(1));
    var generator = CreateGenerator(summary);

    var dataset = (await generator.GenerateAsync(summaryId)).Single();

    Assert.That(dataset.Rows, Has.Count.EqualTo(1));
  }

  [Test]
  public async Task GenerateAsync_UsesParameterFieldsForTypedOptionalColumns()
  {
    var summaryId = Guid.NewGuid().ToString();
    var parameterId = Guid.NewGuid().ToString();
    var summary = CreateCampaignSummary(
      summaryId,
      "Campaign A",
      CreateExperiment(
        DateTime.UnixEpoch,
        DateTime.UnixEpoch.AddSeconds(1),
        parameters:
        [
          CreateParameter("Temperature", AresValueHelper.CreateQuantity(22.5, QuantityType.Temperature, "degC"), uniqueId: Guid.NewGuid().ToString()),
          CreateParameter(string.Empty, AresValueHelper.CreateString("fallback"), uniqueId: parameterId)
        ]));
    var generator = CreateGenerator(summary);

    var dataset = (await generator.GenerateAsync(summaryId)).Single();

    using(Assert.EnterMultipleScope())
    {
      Assert.That(ColumnSchema(dataset, "Input.Temperature").Type, Is.EqualTo(AresDataType.Quantity));
      Assert.That(ColumnSchema(dataset, "Input.Temperature").Optional, Is.True);
      Assert.That(ColumnSchema(dataset, $"Input.{parameterId}").Type, Is.EqualTo(AresDataType.String));
      Assert.That(dataset.Rows.Single().Data.Fields["Input.Temperature"].QuantityValue.Scalar, Is.EqualTo(22.5));
      Assert.That(dataset.Rows.Single().Data.Fields[$"Input.{parameterId}"].StringValue, Is.EqualTo("fallback"));
    }
  }

  [Test]
  public void GenerateAsync_ThrowsWhenAlreadyCanceled()
  {
    var contextFactory = CreateContextFactory();
    using var cancellationTokenSource = new CancellationTokenSource();
    cancellationTokenSource.Cancel();
    var generator = new CampaignDatasetGenerator(contextFactory.Object);

    Assert.ThrowsAsync<OperationCanceledException>(async () => await generator.GenerateAsync("summary-1", cancellationTokenSource.Token));
    contextFactory.Verify(factory => factory.CreateDbContextAsync(It.IsAny<CancellationToken>()), Times.Never);
  }

  private static CampaignDatasetGenerator CreateGenerator(CampaignExecutionSummary summary)
  {
    var options = CreateContextOptions();
    using(var context = new CoreDatabaseContext(options))
    {
      context.CampaignExecutionSummaries.Add(summary);
      context.SaveChanges();
    }

    return new CampaignDatasetGenerator(CreateContextFactory(options).Object);
  }

  private static Mock<IDbContextFactory<CoreDatabaseContext>> CreateContextFactory(DbContextOptions<CoreDatabaseContext> options = null)
  {
    var contextOptions = options ?? CreateContextOptions();
    var contextFactory = new Mock<IDbContextFactory<CoreDatabaseContext>>();
    contextFactory
      .Setup(factory => factory.CreateDbContextAsync(It.IsAny<CancellationToken>()))
      .ReturnsAsync(() => new CoreDatabaseContext(contextOptions));
    return contextFactory;
  }

  private static DbContextOptions<CoreDatabaseContext> CreateContextOptions()
  {
    return new DbContextOptionsBuilder<CoreDatabaseContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;
  }

  private static AresValueSchema ColumnSchema(AresDataset dataset, string columnName)
  {
    return dataset.Columns.Single(column => column.Name == columnName).Schema;
  }

  private static CampaignExecutionSummary CreateCampaignSummary(
    string uniqueId,
    string campaignName,
    params ExperimentExecutionSummary[] experiments)
  {
    var summary = new CampaignExecutionSummary
    {
      UniqueId = uniqueId,
      CampaignId = "campaign-id",
      CampaignName = campaignName,
      ExecutionInfo = new ExecutionInfo
      {
        TimeStarted = Timestamp.FromDateTime(DateTime.UnixEpoch),
        TimeFinished = Timestamp.FromDateTime(DateTime.UnixEpoch.AddSeconds(10))
      }
    };
    summary.ExperimentSummaries.AddRange(experiments);
    return summary;
  }

  private static ExperimentExecutionSummary CreateExperiment(
    DateTime timeStarted,
    DateTime timeFinished,
    double? analysisResult = null,
    (string Name, AresValue Value)[] resultFields = null,
    Parameter[] parameters = null)
  {
    var overview = new ExperimentOverview
    {
      Result = new AresStruct()
    };

    if(analysisResult is not null)
      overview.AnalysisOverview = new AnalysisOverview { Result = analysisResult.Value };

    foreach(var field in resultFields ?? [])
    {
      overview.Result.Fields[field.Name] = field.Value;
    }

    overview.Parameters.AddRange(parameters ?? []);

    return new ExperimentExecutionSummary
    {
      UniqueId = Guid.NewGuid().ToString(),
      ExecutionInfo = new ExecutionInfo
      {
        TimeStarted = Timestamp.FromDateTime(timeStarted),
        TimeFinished = Timestamp.FromDateTime(timeFinished)
      },
      ExperimentOverview = overview
    };
  }

  private static Parameter CreateParameter(string metadataName, AresValue value, string uniqueId = "", long index = 0)
  {
    var parameter = new Parameter
    {
      UniqueId = uniqueId,
      Metadata = string.IsNullOrEmpty(metadataName)
        ? null
        : new ParameterMetadata { Name = metadataName },
      Index = index
    };

    parameter.SetLiteralSource(value);

    return parameter;
  }
}
