using Ares.Core.DataManagement.DataMappers;
using Ares.Datamodel;
using Ares.Datamodel.Analyzing;
using Ares.Datamodel.Analyzing.Remote;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Planning;
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
      CreateExperiment(secondStart, secondEnd, analysisResult: new List<Objective> { new Objective { ObjectiveName = "Result", ObjectiveValue = AresValueHelper.CreateNumber(2.5) } }),
      CreateExperiment(firstStart, firstEnd, analysisResult: new List<Objective> { new Objective { ObjectiveName = "Result", ObjectiveValue = AresValueHelper.CreateNumber(1.5) } }));
    var generator = CreateGenerator(summary);

    var dataset = GetDataset(await generator.GenerateAsync(summaryId), "Experiments");

    using(Assert.EnterMultipleScope())
    {
      Assert.That(dataset.Name, Is.EqualTo("Experiments"));
      Assert.That(dataset.Columns.Take(5).Select(column => column.Name), Is.EqualTo([
        "Experiment Number",
        "Experiment Template",
        "Time Started",
        "Time Finished",
        "Duration Seconds"
        ]));
      Assert.That(ColumnSchema(dataset, "Experiment Number").Type, Is.EqualTo(AresDataType.Int));
      Assert.That(ColumnSchema(dataset, "Time Started").Type, Is.EqualTo(AresDataType.Timestamp));
      Assert.That(ColumnSchema(dataset, "Time Finished").Type, Is.EqualTo(AresDataType.Timestamp));
      Assert.That(ColumnSchema(dataset, "Duration Seconds").Type, Is.EqualTo(AresDataType.Number));
      Assert.That(ColumnSchema(dataset, "Objective.Result").Optional, Is.True);
      Assert.That(dataset.Rows[0].Data.Fields["Experiment Number"].IntValue, Is.EqualTo(1));
      Assert.That(dataset.Rows[0].Data.Fields["Time Started"].TimestampValue, Is.EqualTo(Timestamp.FromDateTime(firstStart)));
      Assert.That(dataset.Rows[0].Data.Fields["Time Finished"].TimestampValue, Is.EqualTo(Timestamp.FromDateTime(firstEnd)));
      Assert.That(dataset.Rows[0].Data.Fields["Duration Seconds"].NumberValue, Is.EqualTo(1));
      Assert.That(dataset.Rows[0].Data.Fields["Objective.Result"].NumberValue, Is.EqualTo(1.5));
      Assert.That(dataset.Rows[1].Data.Fields["Experiment Number"].IntValue, Is.EqualTo(2));
      Assert.That(dataset.Rows[1].Data.Fields["Time Started"].TimestampValue, Is.EqualTo(Timestamp.FromDateTime(secondStart)));
      Assert.That(dataset.Rows[1].Data.Fields["Time Finished"].TimestampValue, Is.EqualTo(Timestamp.FromDateTime(secondEnd)));
      Assert.That(dataset.Rows[1].Data.Fields["Duration Seconds"].NumberValue, Is.EqualTo(1));
      Assert.That(dataset.Rows[1].Data.Fields["Objective.Result"].NumberValue, Is.EqualTo(2.5));
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

    var dataset = GetDataset(await generator.GenerateAsync(summaryId), "Experiments");
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
  public async Task GenerateAsync_IncludesExperimentTemplate()
  {
    var summaryId = Guid.NewGuid().ToString();
    var experiment = CreateExperiment(DateTime.UnixEpoch, DateTime.UnixEpoch.AddSeconds(1));
    experiment.ExperimentOverview.Template = new ExperimentTemplate { Name = "Template A" };
    var generator = CreateGenerator(CreateCampaignSummary(summaryId, "Campaign A", experiment));

    var row = GetDataset(await generator.GenerateAsync(summaryId), "Experiments").Rows.Single();

    Assert.That(row.Data.Fields["Experiment Template"].StringValue, Is.EqualTo("Template A"));
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

    var dataset = GetDataset(await generator.GenerateAsync(summaryId), "Experiments");

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

    var dataset = GetDataset(await generator.GenerateAsync(summaryId), "Experiments");

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

    var dataset = GetDataset(await generator.GenerateAsync(summaryId), "Experiments");

    Assert.That(dataset.Rows, Has.Count.EqualTo(1));
  }

  [Test]
  public async Task GenerateAsync_ReturnsAllCampaignDatasets()
  {
    var summaryId = Guid.NewGuid().ToString();
    var generator = CreateGenerator(CreateCampaignSummary(
      summaryId,
      "Campaign A",
      CreateExperiment(DateTime.UnixEpoch, DateTime.UnixEpoch.AddSeconds(1))));

    var datasets = await generator.GenerateAsync(summaryId);

    using(Assert.EnterMultipleScope())
    {
      Assert.That(datasets.Select(dataset => dataset.Name), Is.EqualTo([
        "Experiments",
        "Commands",
        "Planner Transactions",
        "Analyzer Transactions"
      ]));
      Assert.That(GetDataset(datasets, "Planner Transactions").Rows, Is.Empty);
      Assert.That(GetDataset(datasets, "Analyzer Transactions").Rows, Is.Empty);
    }
  }

  [Test]
  public async Task GenerateAsync_CreatesCommandRowsAndFixedColumns()
  {
    var summaryId = Guid.NewGuid().ToString();
    var commandStart = DateTime.UnixEpoch.AddSeconds(12);
    var commandEnd = DateTime.UnixEpoch.AddSeconds(15);
    var command = CreateCommand(
      commandStart,
      commandEnd,
      commandName: "Aspirate",
      commandDescription: "Move liquid",
      varName: "aspirateResult",
      statusCode: CommandStatusCode.CommandSuccess,
      result: new CommandResult
      {
        Success = true,
        Result = AresValueHelper.CreateNumber(10),
        StatusCode = CommandStatusCode.CommandFailed,
        Error = "warning"
      });
    var step = CreateStep("step-id", DateTime.UnixEpoch.AddSeconds(11), DateTime.UnixEpoch.AddSeconds(16), command);
    var experiment = CreateExperiment(DateTime.UnixEpoch.AddSeconds(10), DateTime.UnixEpoch.AddSeconds(20), steps: [step]);
    var generator = CreateGenerator(CreateCampaignSummary(summaryId, "Campaign A", experiment));

    var dataset = GetDataset(await generator.GenerateAsync(summaryId), "Commands");
    var row = dataset.Rows.Single();

    using(Assert.EnterMultipleScope())
    {
      Assert.That(dataset.Columns.Take(13).Select(column => column.Name), Is.EqualTo([
        "Experiment Number",
        "Step Number",
        "Command Number",
        "Command Name",
        "Command Description",
        "Output Variable Name",
        "Time Started",
        "Time Finished",
        "Duration Seconds",
        "Status",
        "Success",
        "Error",
        "Output"
      ]));
      Assert.That(ColumnSchema(dataset, "Experiment Number").Type, Is.EqualTo(AresDataType.Int));
      Assert.That(ColumnSchema(dataset, "Step Number").Type, Is.EqualTo(AresDataType.Int));
      Assert.That(ColumnSchema(dataset, "Command Number").Type, Is.EqualTo(AresDataType.Int));
      Assert.That(ColumnSchema(dataset, "Status").Type, Is.EqualTo(AresDataType.String));
      Assert.That(ColumnSchema(dataset, "Success").Type, Is.EqualTo(AresDataType.Boolean));
      Assert.That(row.Data.Fields["Experiment Number"].IntValue, Is.EqualTo(1));
      Assert.That(row.Data.Fields["Step Number"].IntValue, Is.EqualTo(1));
      Assert.That(row.Data.Fields["Command Number"].IntValue, Is.EqualTo(1));
      Assert.That(row.Data.Fields["Command Name"].StringValue, Is.EqualTo("Aspirate"));
      Assert.That(row.Data.Fields["Command Description"].StringValue, Is.EqualTo("Move liquid"));
      Assert.That(row.Data.Fields["Output Variable Name"].StringValue, Is.EqualTo("aspirateResult"));
      Assert.That(row.Data.Fields["Time Started"].TimestampValue, Is.EqualTo(Timestamp.FromDateTime(commandStart)));
      Assert.That(row.Data.Fields["Time Finished"].TimestampValue, Is.EqualTo(Timestamp.FromDateTime(commandEnd)));
      Assert.That(row.Data.Fields["Duration Seconds"].NumberValue, Is.EqualTo(3));
      Assert.That(row.Data.Fields["Status"].StringValue, Is.EqualTo(CommandStatusCode.CommandSuccess.ToString()));
      Assert.That(row.Data.Fields["Success"].BoolValue, Is.True);
      Assert.That(row.Data.Fields["Error"].StringValue, Is.EqualTo("warning"));
      Assert.That(row.Data.Fields["Output"].NumberValue, Is.EqualTo(10));
    }
  }

  [Test]
  public async Task GenerateAsync_SortsCommandRowsByExperimentStepAndCommandTime()
  {
    var summaryId = Guid.NewGuid().ToString();
    var firstCommand = CreateCommand(DateTime.UnixEpoch.AddSeconds(2), DateTime.UnixEpoch.AddSeconds(3), commandName: "First");
    var secondCommand = CreateCommand(DateTime.UnixEpoch.AddSeconds(4), DateTime.UnixEpoch.AddSeconds(5), commandName: "Second");
    var firstStep = CreateStep("first-step", DateTime.UnixEpoch.AddSeconds(1), DateTime.UnixEpoch.AddSeconds(6), secondCommand, firstCommand);
    var secondStep = CreateStep("second-step", DateTime.UnixEpoch.AddSeconds(7), DateTime.UnixEpoch.AddSeconds(8), CreateCommand(DateTime.UnixEpoch.AddSeconds(7), DateTime.UnixEpoch.AddSeconds(8), commandName: "Third"));
    var laterExperiment = CreateExperiment(DateTime.UnixEpoch.AddSeconds(20), DateTime.UnixEpoch.AddSeconds(21), steps: [
      CreateStep("later-step", DateTime.UnixEpoch.AddSeconds(20), DateTime.UnixEpoch.AddSeconds(21), CreateCommand(DateTime.UnixEpoch.AddSeconds(20), DateTime.UnixEpoch.AddSeconds(21), commandName: "Fourth"))
    ]);
    var earlierExperiment = CreateExperiment(DateTime.UnixEpoch, DateTime.UnixEpoch.AddSeconds(10), steps: [secondStep, firstStep]);
    var generator = CreateGenerator(CreateCampaignSummary(summaryId, "Campaign A", laterExperiment, earlierExperiment));

    var dataset = GetDataset(await generator.GenerateAsync(summaryId), "Commands");

    Assert.That(dataset.Rows.Select(row => row.Data.Fields["Command Name"].StringValue), Is.EqualTo(["First", "Second", "Third", "Fourth"]));
  }

  [Test]
  public async Task GenerateAsync_LeavesOutputVariableNameEmptyWhenCommandHasNoVariableName()
  {
    var summaryId = Guid.NewGuid().ToString();
    var command = CreateCommand(DateTime.UnixEpoch, DateTime.UnixEpoch.AddSeconds(1));
    var experiment = CreateExperiment(
      DateTime.UnixEpoch,
      DateTime.UnixEpoch.AddSeconds(2),
      steps: [CreateStep("step", DateTime.UnixEpoch, DateTime.UnixEpoch.AddSeconds(2), command)]);
    var generator = CreateGenerator(CreateCampaignSummary(summaryId, "Campaign A", experiment));

    var dataset = GetDataset(await generator.GenerateAsync(summaryId), "Commands");

    using(Assert.EnterMultipleScope())
    {
      Assert.That(ColumnSchema(dataset, "Output Variable Name").Optional, Is.True);
      Assert.That(dataset.Rows.Single().Data.Fields.ContainsKey("Output Variable Name"), Is.False);
    }
  }

  [Test]
  public async Task GenerateAsync_FlattensStructCommandOutputs()
  {
    var summaryId = Guid.NewGuid().ToString();
    var output = AresValueHelper.CreateStruct();
    output.StructValue.Fields["Measurement"] = AresValueHelper.CreateStruct();
    output.StructValue.Fields["Measurement"].StructValue.Fields["Mass"] = AresValueHelper.CreateQuantity(4.5, QuantityType.Mass, "g");
    var command = CreateCommand(DateTime.UnixEpoch, DateTime.UnixEpoch.AddSeconds(1), result: new CommandResult { Result = output });
    var generator = CreateGenerator(CreateCampaignSummary(
      summaryId,
      "Campaign A",
      CreateExperiment(DateTime.UnixEpoch, DateTime.UnixEpoch.AddSeconds(2), steps: [CreateStep("step", DateTime.UnixEpoch, DateTime.UnixEpoch.AddSeconds(2), command)])));

    var dataset = GetDataset(await generator.GenerateAsync(summaryId), "Commands");

    using(Assert.EnterMultipleScope())
    {
      Assert.That(dataset.Columns.Select(column => column.Name), Does.Contain("Output.Measurement.Mass"));
      Assert.That(dataset.Rows.Single().Data.Fields["Output.Measurement.Mass"].QuantityValue.Scalar, Is.EqualTo(4.5));
    }
  }

  [Test]
  public async Task GenerateAsync_UsesAnySchemaForConflictingCommandOutputTypes()
  {
    var summaryId = Guid.NewGuid().ToString();
    var generator = CreateGenerator(CreateCampaignSummary(
      summaryId,
      "Campaign A",
      CreateExperiment(DateTime.UnixEpoch, DateTime.UnixEpoch.AddSeconds(4), steps: [
        CreateStep(
          "step",
          DateTime.UnixEpoch,
          DateTime.UnixEpoch.AddSeconds(4),
          CreateCommand(DateTime.UnixEpoch, DateTime.UnixEpoch.AddSeconds(1), result: new CommandResult { Result = AresValueHelper.CreateNumber(1) }),
          CreateCommand(DateTime.UnixEpoch.AddSeconds(2), DateTime.UnixEpoch.AddSeconds(3), result: new CommandResult { Result = AresValueHelper.CreateString("one") }))
      ])));

    var dataset = GetDataset(await generator.GenerateAsync(summaryId), "Commands");

    Assert.That(ColumnSchema(dataset, "Output").Type, Is.EqualTo(AresDataType.Any));
  }

  [Test]
  public async Task GenerateAsync_LeavesCommandOutputColumnsEmptyWhenResultIsMissing()
  {
    var summaryId = Guid.NewGuid().ToString();
    var commandWithoutResult = CreateCommand(DateTime.UnixEpoch, DateTime.UnixEpoch.AddSeconds(1), result: null);
    var commandWithResult = CreateCommand(DateTime.UnixEpoch.AddSeconds(2), DateTime.UnixEpoch.AddSeconds(3), result: new CommandResult { Result = AresValueHelper.CreateNumber(2) });
    var generator = CreateGenerator(CreateCampaignSummary(
      summaryId,
      "Campaign A",
      CreateExperiment(DateTime.UnixEpoch, DateTime.UnixEpoch.AddSeconds(4), steps: [
        CreateStep("step", DateTime.UnixEpoch, DateTime.UnixEpoch.AddSeconds(4), commandWithoutResult, commandWithResult)
      ])));

    var dataset = GetDataset(await generator.GenerateAsync(summaryId), "Commands");

    using(Assert.EnterMultipleScope())
    {
      Assert.That(dataset.Rows[0].Data.Fields.ContainsKey("Output"), Is.False);
      Assert.That(dataset.Rows[1].Data.Fields["Output"].NumberValue, Is.EqualTo(2));
    }
  }

  [Test]
  public async Task GenerateAsync_IncludesResolvedCommandInputsBeforeOutputs()
  {
    var summaryId = Guid.NewGuid().ToString();
    var structValue = AresValueHelper.CreateStruct();
    structValue.StructValue.Fields["Temperature"] = AresValueHelper.CreateNumber(22.5);
    var literal = CreateParameter("Literal", AresValueHelper.CreateString("literal"));
    var planned = CreateParameter("Planned", AresValueHelper.CreateNumber(1));
    planned.SetPlannedSource(new ParameterMetadata { Name = "Planned" });
    planned.SetResolvedValue(AresValueHelper.CreateNumber(2));
    var environment = CreateParameter("Environment", AresValueHelper.CreateString("initial"));
    environment.SetEnvironmentSource(VariableType.VarUnspecified, "environment");
    environment.SetResolvedValue(structValue);
    var variable = CreateParameter("Variable", AresValueHelper.CreateString("initial"));
    variable.SetCommandVariableSource("previous");
    variable.SetResolvedValue(AresValueHelper.CreateList([AresValueHelper.CreateNumber(3), AresValueHelper.CreateNumber(4)]));
    var template = CreateCommandTemplate("command-template", literal, planned, environment, variable);
    var command = CreateCommand(
      DateTime.UnixEpoch,
      DateTime.UnixEpoch.AddSeconds(1),
      result: new CommandResult { Result = AresValueHelper.CreateNumber(10) },
      templateId: template.UniqueId);
    var experiment = CreateExperiment(
      DateTime.UnixEpoch,
      DateTime.UnixEpoch.AddSeconds(2),
      steps: [CreateStep("step", DateTime.UnixEpoch, DateTime.UnixEpoch.AddSeconds(2), command)]);
    SetCommandTemplates(experiment, template);
    var generator = CreateGenerator(CreateCampaignSummary(summaryId, "Campaign A", experiment));

    var dataset = GetDataset(await generator.GenerateAsync(summaryId), "Commands");
    var columns = dataset.Columns.Select(column => column.Name).ToArray();
    var row = dataset.Rows.Single();

    using(Assert.EnterMultipleScope())
    {
      Assert.That(columns, Does.Contain("Input.Literal"));
      Assert.That(columns, Does.Contain("Planned"));
      Assert.That(columns, Does.Contain("Input.Environment.Temperature"));
      Assert.That(columns, Does.Contain("Input.Variable"));
      Assert.That(Array.IndexOf(columns, "Input.Literal"), Is.LessThan(Array.IndexOf(columns, "Output")));
      Assert.That(row.Data.Fields["Input.Literal"].StringValue, Is.EqualTo("literal"));
      Assert.That(row.Data.Fields["Planned"].NumberValue, Is.EqualTo(2));
      Assert.That(row.Data.Fields["Input.Environment.Temperature"].NumberValue, Is.EqualTo(22.5));
      Assert.That(row.Data.Fields["Input.Variable"].ListValue.Values.Select(value => value.NumberValue), Is.EqualTo([3, 4]));
      Assert.That(row.Data.Fields["Output"].NumberValue, Is.EqualTo(10));
    }
  }

  [Test]
  public async Task GenerateAsync_MatchesCommandInputsByTemplateId()
  {
    var summaryId = Guid.NewGuid().ToString();
    var firstTemplate = CreateCommandTemplate("first-template", CreateParameter("Value", AresValueHelper.CreateString("first")));
    var secondTemplate = CreateCommandTemplate("second-template", CreateParameter("Value", AresValueHelper.CreateString("second")));
    var unmatchedCommand = CreateCommand(DateTime.UnixEpoch, DateTime.UnixEpoch.AddSeconds(1), commandName: "Unmatched", templateId: Guid.NewGuid().ToString());
    var matchedCommand = CreateCommand(DateTime.UnixEpoch.AddSeconds(2), DateTime.UnixEpoch.AddSeconds(3), commandName: "Matched", templateId: secondTemplate.UniqueId);
    var experiment = CreateExperiment(
      DateTime.UnixEpoch,
      DateTime.UnixEpoch.AddSeconds(4),
      steps: [CreateStep("step", DateTime.UnixEpoch, DateTime.UnixEpoch.AddSeconds(4), unmatchedCommand, matchedCommand)]);
    SetCommandTemplates(experiment, firstTemplate, secondTemplate);
    var generator = CreateGenerator(CreateCampaignSummary(summaryId, "Campaign A", experiment));

    var dataset = GetDataset(await generator.GenerateAsync(summaryId), "Commands");

    using(Assert.EnterMultipleScope())
    {
      Assert.That(dataset.Rows[0].Data.Fields.ContainsKey("Input.Value"), Is.False);
      Assert.That(dataset.Rows[1].Data.Fields["Input.Value"].StringValue, Is.EqualTo("second"));
    }
  }

  [Test]
  public async Task GenerateAsync_UsesAnySchemaForConflictingCommandInputTypes()
  {
    var summaryId = Guid.NewGuid().ToString();
    var firstTemplate = CreateCommandTemplate("first-template", CreateParameter("Value", AresValueHelper.CreateNumber(1)));
    var secondTemplate = CreateCommandTemplate("second-template", CreateParameter("Value", AresValueHelper.CreateString("one")));
    var experiment = CreateExperiment(
      DateTime.UnixEpoch,
      DateTime.UnixEpoch.AddSeconds(4),
      steps: [
        CreateStep(
          "step",
          DateTime.UnixEpoch,
          DateTime.UnixEpoch.AddSeconds(4),
          CreateCommand(DateTime.UnixEpoch, DateTime.UnixEpoch.AddSeconds(1), templateId: firstTemplate.UniqueId),
          CreateCommand(DateTime.UnixEpoch.AddSeconds(2), DateTime.UnixEpoch.AddSeconds(3), templateId: secondTemplate.UniqueId))
      ]);
    SetCommandTemplates(experiment, firstTemplate, secondTemplate);
    var generator = CreateGenerator(CreateCampaignSummary(summaryId, "Campaign A", experiment));

    var dataset = GetDataset(await generator.GenerateAsync(summaryId), "Commands");

    using(Assert.EnterMultipleScope())
    {
      Assert.That(ColumnSchema(dataset, "Input.Value").Type, Is.EqualTo(AresDataType.Any));
      Assert.That(ColumnSchema(dataset, "Input.Value").Optional, Is.True);
    }
  }

  [Test]
  public async Task GenerateAsync_ExcludesStartupAndCloseoutCommands()
  {
    var summaryId = Guid.NewGuid().ToString();
    var summary = CreateCampaignSummary(
      summaryId,
      "Campaign A",
      CreateExperiment(DateTime.UnixEpoch, DateTime.UnixEpoch.AddSeconds(1), steps: [
        CreateStep("experiment-step", DateTime.UnixEpoch, DateTime.UnixEpoch.AddSeconds(1), CreateCommand(DateTime.UnixEpoch, DateTime.UnixEpoch.AddSeconds(1), commandName: "Experiment"))
      ]));
    summary.StartupExecutionSummary = CreateExperiment(DateTime.UnixEpoch, DateTime.UnixEpoch.AddSeconds(1), steps: [
      CreateStep("startup-step", DateTime.UnixEpoch, DateTime.UnixEpoch.AddSeconds(1), CreateCommand(DateTime.UnixEpoch, DateTime.UnixEpoch.AddSeconds(1), commandName: "Startup"))
    ]);
    summary.CloseoutExecutionSummary = CreateExperiment(DateTime.UnixEpoch, DateTime.UnixEpoch.AddSeconds(1), steps: [
      CreateStep("closeout-step", DateTime.UnixEpoch, DateTime.UnixEpoch.AddSeconds(1), CreateCommand(DateTime.UnixEpoch, DateTime.UnixEpoch.AddSeconds(1), commandName: "Closeout"))
    ]);
    var generator = CreateGenerator(summary);

    var dataset = GetDataset(await generator.GenerateAsync(summaryId), "Commands");

    Assert.That(dataset.Rows.Select(row => row.Data.Fields["Command Name"].StringValue), Is.EqualTo(["Experiment"]));
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

    var dataset = GetDataset(await generator.GenerateAsync(summaryId), "Experiments");

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
  public async Task GenerateAsync_CreatesPlannerTransactionRows()
  {
    var summaryId = Guid.NewGuid().ToString();
    var experiment = CreateExperiment(DateTime.UnixEpoch, DateTime.UnixEpoch.AddSeconds(5));
    var nestedOutput = AresValueHelper.CreateStruct();
    nestedOutput.StructValue.Fields["Value"] = AresValueHelper.CreateInt(100);

    var nestConcentration = AresValueHelper.CreateStruct();
    nestConcentration.StructValue.Fields["Value"] = AresValueHelper.CreateFloat(1.6);

    // This helper creates Plan 1 with the Temperature output
    var transaction = CreatePlannerTransaction(
      experiment.ExperimentId,
      DateTime.UnixEpoch.AddSeconds(1),
      DateTime.UnixEpoch.AddSeconds(3),
      ("Temperature", nestedOutput),
      ("Concentration", nestConcentration));

    var objectiveOne = new Objective() 
    { 
      ObjectiveValue = AresValueHelper.CreateNumber(1.5), 
      ObjectiveName = "ObjectiveOne" 
    };

    var objectiveTwo = new Objective() 
    { 
      ObjectiveValue = AresValueHelper.CreateNumber(2.5), 
      ObjectiveName = "ObjectiveTwo" 
    };

    var responseOne = new AnalysisData() { AnalysisObjectives = { objectiveOne } };
    var responseTwo = new AnalysisData() { AnalysisObjectives = { objectiveTwo } };

    var planOne = new Plan() { ErrorString = "planner warning", PlanningOutcome = Outcome.Warning };
    planOne.PlannedParameters.Add(new PlannedParameter() { ParameterName = "Temperature", ParameterValue = AresValueHelper.CreateInt(200) });
    planOne.PlannedParameters.Add(new PlannedParameter() { ParameterName = "Concentration", ParameterValue = AresValueHelper.CreateFloat(2.6) });

    var planTwo = new Plan() { ErrorString = "planner error", PlanningOutcome = Outcome.Failure };
    planTwo.PlannedParameters.Add(new PlannedParameter() { ParameterName = "Temperature", ParameterValue = AresValueHelper.CreateInt(300) });
    planTwo.PlannedParameters.Add(new PlannedParameter() { ParameterName = "Concentration", ParameterValue = AresValueHelper.CreateFloat(3.6) });

    transaction.PlanningRequest.AnalysisData.AddRange([responseOne, responseTwo]);
    transaction.PlanningResponse.Plans.AddRange([planOne, planTwo]);

    var generator = CreateGenerator(CreateCampaignSummary(summaryId, "Campaign A", experiment), [transaction]);

    var dataset = GetDataset(await generator.GenerateAsync(summaryId), "Planner Transactions");

    // We now expect 3 distinct rows
    var rows = dataset.Rows.ToList();

    using(Assert.EnterMultipleScope())
    {
      Assert.That(rows, Has.Count.EqualTo(3));

      Assert.That(dataset.Columns.Take(11).Select(column => column.Name), Is.EqualTo([
      "Experiment Number",
      "Plan Number",
      "Planner Name",
      "Planner Type",
      "Planner Version",
      "Time Request Sent",
      "Time Response Received",
      "Duration Seconds",
      "Outcome",
      "Error",
      "Output.Temperature.Value"
      ]));

      // Transaction-level fields should be identical across all 3 rows
      foreach(var row in rows)
      {
        Assert.That(row.Data.Fields["Experiment Number"].IntValue, Is.EqualTo(1));
        Assert.That(row.Data.Fields["Planner Name"].StringValue, Is.EqualTo("Planner A"));
        Assert.That(row.Data.Fields["Duration Seconds"].NumberValue, Is.EqualTo(2));
        Assert.That(row.Data.Fields["Objective.ObjectiveOne"].NumberValue, Is.EqualTo(1.5));
        Assert.That(row.Data.Fields["Objective.ObjectiveTwo"].NumberValue, Is.EqualTo(2.5));
      }

      var rowOne = rows[0];
      Assert.That(rowOne.Data.Fields["Plan Number"].IntValue, Is.EqualTo(1));
      Assert.That(rowOne.Data.Fields["Output.Temperature.Value"].IntValue, Is.EqualTo(100));
      Assert.That(rowOne.Data.Fields["Output.Concentration.Value"].FloatValue, Is.EqualTo(1.6));

      var rowTwo = rows[1];
      Assert.That(rowTwo.Data.Fields["Plan Number"].IntValue, Is.EqualTo(2));
      Assert.That(rowTwo.Data.Fields["Outcome"].StringValue, Is.EqualTo(Outcome.Warning.ToString()));
      Assert.That(rowTwo.Data.Fields["Error"].StringValue, Is.EqualTo("planner warning"));
      Assert.That(rowTwo.Data.Fields["Output.Temperature"].IntValue, Is.EqualTo(200));
      Assert.That(rowTwo.Data.Fields["Output.Concentration"].FloatValue, Is.EqualTo(2.6));

      var rowThree = rows[2];
      Assert.That(rowThree.Data.Fields["Plan Number"].IntValue, Is.EqualTo(3));
      Assert.That(rowThree.Data.Fields["Outcome"].StringValue, Is.EqualTo(Outcome.Failure.ToString()));
      Assert.That(rowThree.Data.Fields["Error"].StringValue, Is.EqualTo("planner error"));
      Assert.That(rowThree.Data.Fields["Output.Temperature"].IntValue, Is.EqualTo(300));
      Assert.That(rowThree.Data.Fields["Output.Concentration"].FloatValue, Is.EqualTo(3.6));
    }
  }

  [Test]
  public async Task GenerateAsync_CreatesAnalyzerTransactionRows()
  {
    var summaryId = Guid.NewGuid().ToString();
    var experiment = CreateExperiment(DateTime.UnixEpoch, DateTime.UnixEpoch.AddSeconds(5));
    var nestedInput = AresValueHelper.CreateStruct();
    nestedInput.StructValue.Fields["Mass"] = AresValueHelper.CreateQuantity(4.5, QuantityType.Mass, "g");
    var transaction = CreateAnalyzerTransaction(
      experiment.ExperimentId,
      DateTime.UnixEpoch.AddSeconds(2),
      DateTime.UnixEpoch.AddSeconds(4),
      ("Measurement", nestedInput));

    transaction.AnalysisResponse = new AnalysisResponse
    {
      Objectives = { new Objective() { ObjectiveName = "Result", ObjectiveValue = AresValueHelper.CreateNumber(9.5) } },
      AnalysisOutcome = Outcome.Success,
      ErrorString = "analysis note"
    };
    var generator = CreateGenerator(CreateCampaignSummary(summaryId, "Campaign A", experiment), analyzerTransactions: [transaction]);

    var dataset = GetDataset(await generator.GenerateAsync(summaryId), "Analyzer Transactions");
    var row = dataset.Rows.Single();

    using(Assert.EnterMultipleScope())
    {
      Assert.That(dataset.Columns.Take(11).Select(column => column.Name), Is.EqualTo([
        "Experiment Number",
        "Analyzer Name",
        "Analyzer Type",
        "Analyzer Version",
        "Time Request Sent",
        "Time Response Received",
        "Duration Seconds",
        "Result",
        "Outcome",
        "Error",
        "Input.Measurement.Mass"
      ]));
      Assert.That(row.Data.Fields["Analyzer Version"].StringValue, Is.EqualTo("2.0"));
      Assert.That(row.Data.Fields["Duration Seconds"].NumberValue, Is.EqualTo(2));
      Assert.That(row.Data.Fields["Objective.Result"].NumberValue, Is.EqualTo(9.5));
      Assert.That(row.Data.Fields["Outcome"].StringValue, Is.EqualTo(Outcome.Success.ToString()));
      Assert.That(row.Data.Fields["Input.Measurement.Mass"].QuantityValue.Scalar, Is.EqualTo(4.5));
    }
  }

  [Test]
  public async Task GenerateAsync_FiltersTransactionsToMatchedCampaignExperimentsAndWindow()
  {
    var summaryId = Guid.NewGuid().ToString();
    var experiment = CreateExperiment(DateTime.UnixEpoch, DateTime.UnixEpoch.AddSeconds(5));
    var valid = CreatePlannerTransaction(experiment.ExperimentId, DateTime.UnixEpoch.AddSeconds(1), DateTime.UnixEpoch.AddSeconds(2));
    var wrongCampaign = CreatePlannerTransaction(experiment.ExperimentId, DateTime.UnixEpoch.AddSeconds(1), DateTime.UnixEpoch.AddSeconds(2));
    wrongCampaign.PlanningRequest.Metadata.CampaignId = "other-campaign";
    var wrongExperiment = CreatePlannerTransaction("other-experiment", DateTime.UnixEpoch.AddSeconds(1), DateTime.UnixEpoch.AddSeconds(2));
    var outsideWindow = CreatePlannerTransaction(experiment.ExperimentId, DateTime.UnixEpoch.AddSeconds(9), DateTime.UnixEpoch.AddSeconds(11));
    var generator = CreateGenerator(
      CreateCampaignSummary(summaryId, "Campaign A", experiment),
      [valid, wrongCampaign, wrongExperiment, outsideWindow]);

    var dataset = GetDataset(await generator.GenerateAsync(summaryId), "Planner Transactions");

    Assert.That(dataset.Rows, Has.Count.EqualTo(1));
  }

  [Test]
  public async Task GenerateAsync_UsesAnySchemaForConflictingTransactionValues()
  {
    var summaryId = Guid.NewGuid().ToString();
    var experiment = CreateExperiment(DateTime.UnixEpoch, DateTime.UnixEpoch.AddSeconds(5));
    var first = CreatePlannerTransaction(
      experiment.ExperimentId,
      DateTime.UnixEpoch.AddSeconds(1),
      DateTime.UnixEpoch.AddSeconds(2),
      ("Value", AresValueHelper.CreateNumber(1)));
    var second = CreatePlannerTransaction(
      experiment.ExperimentId,
      DateTime.UnixEpoch.AddSeconds(3),
      DateTime.UnixEpoch.AddSeconds(4),
      ("Value", AresValueHelper.CreateString("one")));
    var generator = CreateGenerator(CreateCampaignSummary(summaryId, "Campaign A", experiment), [first, second]);

    var dataset = GetDataset(await generator.GenerateAsync(summaryId), "Planner Transactions");

    using(Assert.EnterMultipleScope())
    {
      Assert.That(ColumnSchema(dataset, "Output.Value").Type, Is.EqualTo(AresDataType.Any));
      Assert.That(ColumnSchema(dataset, "Output.Value").Optional, Is.True);
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

  private static CampaignDatasetGenerator CreateGenerator(
    CampaignExecutionSummary summary,
    PlannerTransaction[] plannerTransactions = null,
    AnalyzerTransaction[] analyzerTransactions = null)
  {
    var options = CreateContextOptions();
    using(var context = new CoreDatabaseContext(options))
    {
      context.CampaignExecutionSummaries.Add(summary);
      context.PlannerTransactions.AddRange(plannerTransactions ?? []);
      context.AnalyzerTransactions.AddRange(analyzerTransactions ?? []);
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

  private static AresDataset GetDataset(IEnumerable<AresDataset> datasets, string name)
  {
    return datasets.Single(dataset => dataset.Name == name);
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
    List<Objective>? analysisResult = null,
    (string Name, AresValue Value)[] resultFields = null,
    Parameter[] parameters = null,
    StepExecutionSummary[] steps = null)
  {
    var overview = new ExperimentOverview
    {
      Result = new AresStruct()
    };

    if(analysisResult is not null)
    {
      overview.AnalysisOverview = new AnalysisOverview();
      overview.AnalysisOverview.Objectives.AddRange(analysisResult);

    }
      

    foreach(var field in resultFields ?? [])
    {
      overview.Result.Fields[field.Name] = field.Value;
    }

    overview.Parameters.AddRange(parameters ?? []);

    var experiment = new ExperimentExecutionSummary
    {
      UniqueId = Guid.NewGuid().ToString(),
      ExperimentId = Guid.NewGuid().ToString(),
      ExecutionInfo = new ExecutionInfo
      {
        TimeStarted = Timestamp.FromDateTime(timeStarted),
        TimeFinished = Timestamp.FromDateTime(timeFinished)
      },
      ExperimentOverview = overview
    };
    experiment.StepSummaries.AddRange(steps ?? []);
    return experiment;
  }

  private static StepExecutionSummary CreateStep(
    string stepId,
    DateTime timeStarted,
    DateTime timeFinished,
    params CommandExecutionSummary[] commands)
  {
    var step = new StepExecutionSummary
    {
      UniqueId = Guid.NewGuid().ToString(),
      StepId = stepId,
      ExecutionInfo = new ExecutionInfo
      {
        TimeStarted = Timestamp.FromDateTime(timeStarted),
        TimeFinished = Timestamp.FromDateTime(timeFinished)
      }
    };
    step.CommandSummaries.AddRange(commands);
    return step;
  }

  private static CommandExecutionSummary CreateCommand(
    DateTime timeStarted,
    DateTime timeFinished,
    string commandName = "",
    string commandDescription = "",
    string varName = "",
    CommandStatusCode statusCode = CommandStatusCode.StatusUnspecified,
    CommandResult result = null,
    string templateId = "")
  {
    var command = new CommandExecutionSummary
    {
      UniqueId = Guid.NewGuid().ToString(),
      TemplateId = templateId,
      CommandName = commandName,
      CommandDescription = commandDescription,
      VarName = varName,
      StatusCode = statusCode,
      Result = result,
      ExecutionInfo = new ExecutionInfo
      {
        TimeStarted = Timestamp.FromDateTime(timeStarted),
        TimeFinished = Timestamp.FromDateTime(timeFinished)
      }
    };

    return command;
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

  private static CommandTemplate CreateCommandTemplate(string uniqueId, params Parameter[] parameters)
  {
    foreach(var parameter in parameters)
    {
      if(string.IsNullOrWhiteSpace(parameter.UniqueId))
        parameter.UniqueId = Guid.NewGuid().ToString();

      if(parameter.Metadata is not null && string.IsNullOrWhiteSpace(parameter.Metadata.UniqueId))
        parameter.Metadata.UniqueId = Guid.NewGuid().ToString();
    }

    var template = new CommandTemplate
    {
      UniqueId = Guid.NewGuid().ToString(),
      DeviceCommand = new DeviceCommand
      {
        Metadata = new CommandMetadata
        {
          UniqueId = Guid.NewGuid().ToString(),
          Name = uniqueId
        }
      }
    };
    template.ArgumentBindings.AddRange(parameters);
    return template;
  }

  private static void SetCommandTemplates(ExperimentExecutionSummary experiment, params CommandTemplate[] commandTemplates)
  {
    var stepTemplate = new StepTemplate
    {
      UniqueId = Guid.NewGuid().ToString(),
      Name = "Step"
    };
    stepTemplate.CommandTemplates.AddRange(commandTemplates);
    experiment.ExperimentOverview.Template = new ExperimentTemplate
    {
      UniqueId = Guid.NewGuid().ToString(),
      Name = "Experiment"
    };
    experiment.ExperimentOverview.Template.StepTemplates.Add(stepTemplate);
  }

  private static PlannerTransaction CreatePlannerTransaction(
    string experimentId,
    DateTime requestSent,
    DateTime responseReceived,
    params (string Name, AresValue Value)[] outputs)
  {
    var transaction = new PlannerTransaction
    {
      UniqueId = Guid.NewGuid().ToString(),
      PlannerName = "Planner A",
      PlannerType = "Remote",
      PlannerVersion = "1.0",
      TimeRequestSent = Timestamp.FromDateTime(requestSent),
      TimeResponseReceived = Timestamp.FromDateTime(responseReceived),
      PlanningRequest = new PlanningRequest
      {
        Metadata = new RequestMetadata
        {
          CampaignId = "campaign-id",
          ExperimentId = experimentId
        }
      },
      PlanningResponse = new PlanningResponse() { ObjectiveStatus = ObjectiveStatus.ObjectiveUnachieved }
    };

    var plan = new Plan();
    plan.PlannedParameters.AddRange(outputs.Select(output => new PlannedParameter
    {
      ParameterName = output.Name,
      ParameterValue = output.Value
    }));

    transaction.PlanningResponse.Plans.Add(plan);
    return transaction;
  }

  private static AnalyzerTransaction CreateAnalyzerTransaction(
    string experimentId,
    DateTime requestSent,
    DateTime responseReceived,
    params (string Name, AresValue Value)[] inputs)
  {
    var transaction = new AnalyzerTransaction
    {
      UniqueId = Guid.NewGuid().ToString(),
      AnalyzerName = "Analyzer A",
      AnalyzerType = "Remote",
      AnalyzerVersion = "2.0",
      TimeRequestSent = Timestamp.FromDateTime(requestSent),
      TimeResponseReceived = Timestamp.FromDateTime(responseReceived),
      AnalysisRequest = new AnalysisRequest
      {
        Inputs = new AresStruct(),
        Metadata = new RequestMetadata
        {
          CampaignId = "campaign-id",
          ExperimentId = experimentId
        }
      },
      AnalysisResponse = new AnalysisResponse()
    };

    foreach(var input in inputs)
    {
      transaction.AnalysisRequest.Inputs.Fields[input.Name] = input.Value;
    }

    return transaction;
  }
}
