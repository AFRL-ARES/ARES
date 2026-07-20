using Ares.Core.Analyzing;
using Ares.Core.CustomCommands;
using Ares.Core.Execution.Executors;
using Ares.Core.Validation.Campaign;
using Ares.Core.Validation.Validators;
using Ares.Datamodel;
using Ares.Datamodel.Analyzing;
using Ares.Datamodel.Analyzing.Remote;
using Ares.Datamodel.Automation;
using Ares.Datamodel.Connection;
using Ares.Datamodel.Templates;
using Moq;

namespace Ares.Core.Tests.Validation;

internal class GoodAnalyzerValidatorTests
{
  [Test]
  public async Task Validate_MapsNestedStructSchemaToAnalyzerInput()
  {
    var experimentTemplate = CreateExperimentTemplate(
      CreateNestedOutputSchema(),
      "result.outer.inner");

    var analyzer = CreateAnalyzer(inputSchema =>
      inputSchema.Fields.TryGetValue("AnalyzerInput", out var input)
      && input.Type == AresDataType.Number);

    var analyzerRepo = CreateAnalyzerRepo(analyzer.Object);

    var result = await GoodAnalyzerValidator.Validate(experimentTemplate, null, analyzerRepo.Object);

    Assert.That(result.Success, Is.True);
    analyzer.Verify(a => a.ValidateInputs(
      It.Is<AresStructSchema>(schema =>
        schema.Fields.ContainsKey("AnalyzerInput")
        && schema.Fields["AnalyzerInput"].Type == AresDataType.Number),
      It.IsAny<CancellationToken>()), Times.Once);
  }

  [Test]
  public async Task Validate_FailsWhenNestedStructPathDoesNotMatchAnalyzerRequiredInput()
  {
    var experimentTemplate = CreateExperimentTemplate(
      CreateNestedOutputSchema(),
      "result.outer.missing");

    var analyzer = CreateAnalyzer(inputSchema =>
      inputSchema.Fields.TryGetValue("AnalyzerInput", out var input)
      && input.Type == AresDataType.Number);

    var analyzerRepo = CreateAnalyzerRepo(analyzer.Object);

    var result = await GoodAnalyzerValidator.Validate(experimentTemplate, null, analyzerRepo.Object);

    Assert.That(result.Success, Is.False);
    Assert.That(result.Messages, Does.Contain("Missing AnalyzerInput"));
    Assert.That(result.Messages, Has.Some.Contains("output member is unavailable"));
  }

  [Test]
  public async Task Validate_MapsSystemCommandOutputSchema()
  {
    var command = new CommandTemplate
    {
      OutputVarName = "timestamp",
      SystemCommand = new SystemCommand { Operation = SystemOperation.GetTimestamp }
    };
    var experimentTemplate = CreateExperimentTemplate(command, "timestamp");
    var analyzer = CreateAnalyzer(
      inputSchema => inputSchema.Fields.TryGetValue("AnalyzerInput", out var input)
        && input.Type == AresDataType.Timestamp,
      AresDataType.Timestamp);

    var result = await GoodAnalyzerValidator.Validate(
      experimentTemplate,
      null,
      CreateAnalyzerRepo(analyzer.Object).Object);

    Assert.That(result.Success, Is.True);
  }

  [Test]
  public async Task Validate_MapsCustomCommandScalarOutputByStableIdCaseInsensitively()
  {
    const string savedId = "A2A403BA-1284-4E1F-8C67-082AF05E879B";
    var experimentTemplate = CreateExperimentTemplate(CreateCustomCommand(savedId), "result");
    var analyzer = CreateAnalyzer(inputSchema =>
      inputSchema.Fields.TryGetValue("AnalyzerInput", out var input)
      && input.Type == AresDataType.Number);
    var schemas = new Dictionary<string, AresValueSchema>
    {
      [savedId.ToLowerInvariant()] = new() { Type = AresDataType.Number }
    };

    var result = await GoodAnalyzerValidator.Validate(
      experimentTemplate,
      null,
      CreateAnalyzerRepo(analyzer.Object).Object,
      schemas);

    Assert.That(result.Success, Is.True);
  }

  [Test]
  public async Task Validate_MapsCustomCommandNestedStructOutput()
  {
    const string commandId = "custom-command-id";
    var experimentTemplate = CreateExperimentTemplate(CreateCustomCommand(commandId), "result.outer.inner");
    var analyzer = CreateAnalyzer(inputSchema =>
      inputSchema.Fields.TryGetValue("AnalyzerInput", out var input)
      && input.Type == AresDataType.Number);

    var result = await GoodAnalyzerValidator.Validate(
      experimentTemplate,
      null,
      CreateAnalyzerRepo(analyzer.Object).Object,
      new Dictionary<string, AresValueSchema> { [commandId] = CreateNestedOutputSchema() });

    Assert.That(result.Success, Is.True);
  }

  [Test]
  public async Task Validate_FailsClearlyWhenCustomCommandIsMissing()
  {
    var experimentTemplate = CreateExperimentTemplate(CreateCustomCommand("deleted-command"), "result");
    var analyzer = CreateAnalyzer(_ => false);

    var result = await GoodAnalyzerValidator.Validate(
      experimentTemplate,
      null,
      CreateAnalyzerRepo(analyzer.Object).Object,
      new Dictionary<string, AresValueSchema>());

    using(Assert.EnterMultipleScope())
    {
      Assert.That(result.Success, Is.False);
      Assert.That(result.Messages, Has.Some.Contains("custom command 'deleted-command'"));
    }
  }

  [TestCase(AresDataType.Unit)]
  [TestCase(AresDataType.UnspecifiedType)]
  public async Task Validate_FailsClearlyWhenOutputSchemaIsUnusable(AresDataType schemaType)
  {
    var experimentTemplate = CreateExperimentTemplate(
      CreateDeviceCommand(new AresValueSchema { Type = schemaType }),
      "result");
    var result = await GoodAnalyzerValidator.Validate(
      experimentTemplate,
      null,
      CreateAnalyzerRepo(CreateAnalyzer(_ => false).Object).Object);

    using(Assert.EnterMultipleScope())
    {
      Assert.That(result.Success, Is.False);
      Assert.That(result.Messages, Has.Some.Contains("does not have a usable output schema"));
    }
  }

  [Test]
  public async Task Validate_FailsClearlyWhenOutputSchemaIsMissing()
  {
    var command = new CommandTemplate
    {
      OutputVarName = "result",
      DeviceCommand = new DeviceCommand
      {
        Metadata = new CommandMetadata { OutputMetadata = new OutputMetadata() }
      }
    };
    var experimentTemplate = CreateExperimentTemplate(command, "result");

    var result = await GoodAnalyzerValidator.Validate(
      experimentTemplate,
      null,
      CreateAnalyzerRepo(CreateAnalyzer(_ => false).Object).Object);

    using(Assert.EnterMultipleScope())
    {
      Assert.That(result.Success, Is.False);
      Assert.That(result.Messages, Has.Some.Contains("does not have a usable output schema"));
    }
  }

  [Test]
  public async Task CampaignValidator_LoadsCurrentCustomCommandsOnce()
  {
    const string commandId = "current-custom-command";
    var experimentTemplate = CreateExperimentTemplate(CreateCustomCommand(commandId), "result");
    var analyzer = CreateAnalyzer(inputSchema => inputSchema.Fields["AnalyzerInput"].Type == AresDataType.Number);
    var persistence = new Mock<ICustomCommandPersistenceService>();
    persistence.Setup(service => service.GetCommandsAsync()).ReturnsAsync([
      new CustomCommandVersion
      {
        CustomCommandId = commandId,
        OutputSchema = new AresValueSchema { Type = AresDataType.Number }
      }
    ]);
    var validator = new GoodAnalyzerCampaignValidator(CreateAnalyzerRepo(analyzer.Object).Object, persistence.Object);

    var result = await validator.Validate(new CampaignTemplate { ExperimentTemplate = experimentTemplate });

    Assert.That(result.Success, Is.True);
    persistence.Verify(service => service.GetCommandsAsync(), Times.Once);
  }

  [Test]
  public async Task CampaignValidator_ReturnsFailureWhenCustomCommandsCannotBeLoaded()
  {
    var experimentTemplate = CreateExperimentTemplate(CreateCustomCommand("custom-command"), "result");
    var persistence = new Mock<ICustomCommandPersistenceService>();
    persistence.Setup(service => service.GetCommandsAsync()).ThrowsAsync(new InvalidOperationException("Database unavailable"));
    var validator = new GoodAnalyzerCampaignValidator(new Mock<IAnalyzerRepo>().Object, persistence.Object);

    var result = await validator.Validate(new CampaignTemplate { ExperimentTemplate = experimentTemplate });

    using(Assert.EnterMultipleScope())
    {
      Assert.That(result.Success, Is.False);
      Assert.That(result.Messages, Has.Some.Contains("Unable to load current custom-command definitions"));
      Assert.That(result.Messages, Has.Some.Contains("Database unavailable"));
    }
  }

  [Test]
  public async Task CampaignValidator_DoesNotLoadCustomCommandsWhenNoAnalyzerIsAssigned()
  {
    var experimentTemplate = CreateExperimentTemplate(CreateCustomCommand("custom-command"), "result");
    experimentTemplate.ClearAnalyzerId();
    var persistence = new Mock<ICustomCommandPersistenceService>();
    var validator = new GoodAnalyzerCampaignValidator(new Mock<IAnalyzerRepo>().Object, persistence.Object);

    var result = await validator.Validate(new CampaignTemplate { ExperimentTemplate = experimentTemplate });

    Assert.That(result.Success, Is.True);
    persistence.Verify(service => service.GetCommandsAsync(), Times.Never);
  }

  private static Mock<IAnalyzerRepo> CreateAnalyzerRepo(IAnalyzer analyzer)
  {
    var analyzerRepo = new Mock<IAnalyzerRepo>();
    analyzerRepo.Setup(repo => repo.GetAnalyzerById("analyzer-id")).Returns(analyzer);
    return analyzerRepo;
  }

  private static Mock<IAnalyzer> CreateAnalyzer(
    Func<AresStructSchema, bool> validateInputSchema,
    AresDataType requiredType = AresDataType.Number)
  {
    var parameters = new AresStructSchema();
    parameters.Fields["AnalyzerInput"] = new AresValueSchema
    {
      Type = requiredType,
      Optional = false
    };

    var analyzer = new Mock<IAnalyzer>();
    analyzer.SetupGet(a => a.Name).Returns("Analyzer");
    analyzer.SetupGet(a => a.AnalyzerState).Returns(State.Active);
    analyzer.Setup(a => a.GetParameters(It.IsAny<CancellationToken>())).ReturnsAsync(parameters);
    analyzer
      .Setup(a => a.ValidateInputs(It.IsAny<AresStructSchema>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync((AresStructSchema inputSchema, CancellationToken _) => new ParameterValidationResult
      {
        Success = validateInputSchema(inputSchema),
        Messages = { validateInputSchema(inputSchema) ? "Valid" : "Missing AnalyzerInput" }
      });

    return analyzer;
  }

  private static ExperimentTemplate CreateExperimentTemplate(AresValueSchema outputSchema, string analyzerMapValue)
    => CreateExperimentTemplate(CreateDeviceCommand(outputSchema), analyzerMapValue);

  private static ExperimentTemplate CreateExperimentTemplate(CommandTemplate command, string analyzerMapValue)
  {
    var experimentTemplate = new ExperimentTemplate
    {
      AnalyzerId = "analyzer-id"
    };
    experimentTemplate.AnalyzerMaps["AnalyzerInput"] = analyzerMapValue;

    var stepTemplate = new StepTemplate();
    stepTemplate.CommandTemplates.Add(command);

    experimentTemplate.StepTemplates.Add(stepTemplate);
    return experimentTemplate;
  }

  private static CommandTemplate CreateDeviceCommand(AresValueSchema outputSchema)
    => new()
    {
      OutputVarName = "result",
      DeviceCommand = new DeviceCommand
      {
        Metadata = new CommandMetadata
        {
          OutputMetadata = new OutputMetadata { DataSchema = outputSchema }
        }
      }
    };

  private static CommandTemplate CreateCustomCommand(string commandId)
    => new()
    {
      OutputVarName = "result",
      CustomCommandInvocation = new CustomCommandInvocation { CustomCommandId = commandId }
    };

  private static AresValueSchema CreateNestedOutputSchema()
  {
    var innerStructSchema = new AresStructSchema();
    innerStructSchema.Fields["inner"] = new AresValueSchema
    {
      Type = AresDataType.Number
    };

    var outerStructSchema = new AresStructSchema();
    outerStructSchema.Fields["outer"] = new AresValueSchema
    {
      Type = AresDataType.Struct,
      StructSchema = innerStructSchema
    };

    return new AresValueSchema
    {
      Type = AresDataType.Struct,
      StructSchema = outerStructSchema
    };
  }
}
