using Ares.Core.Analyzing;
using Ares.Core.Validation.Validators;
using Ares.Datamodel;
using Ares.Datamodel.Analyzing;
using Ares.Datamodel.Analyzing.Remote;
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
  }

  private static Mock<IAnalyzerRepo> CreateAnalyzerRepo(IAnalyzer analyzer)
  {
    var analyzerRepo = new Mock<IAnalyzerRepo>();
    analyzerRepo.Setup(repo => repo.GetAnalyzerById("analyzer-id")).Returns(analyzer);
    return analyzerRepo;
  }

  private static Mock<IAnalyzer> CreateAnalyzer(Func<AresStructSchema, bool> validateInputSchema)
  {
    var parameters = new AresStructSchema();
    parameters.Fields["AnalyzerInput"] = new AresValueSchema
    {
      Type = AresDataType.Number,
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
  {
    var experimentTemplate = new ExperimentTemplate
    {
      AnalyzerId = "analyzer-id"
    };
    experimentTemplate.AnalyzerMaps["AnalyzerInput"] = analyzerMapValue;

    var stepTemplate = new StepTemplate();
    stepTemplate.CommandTemplates.Add(new CommandTemplate
    {
      OutputVarName = "result",
      Metadata = new CommandMetadata
      {
        OutputMetadata = new OutputMetadata
        {
          DataSchema = outputSchema
        }
      }
    });

    experimentTemplate.StepTemplates.Add(stepTemplate);
    return experimentTemplate;
  }

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
