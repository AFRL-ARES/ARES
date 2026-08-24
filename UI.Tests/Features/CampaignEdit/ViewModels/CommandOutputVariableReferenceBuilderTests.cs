using Ares.Datamodel;
using Ares.Datamodel.Templates;
using UI.Features.CampaignEdit.ViewModels;

namespace UI.Tests.Features.CampaignEdit.ViewModels;

public class CommandOutputVariableReferenceBuilderTests
{
  private static readonly IReadOnlyDictionary<string, AresValueSchema> NoCustomCommandSchemas
    = new Dictionary<string, AresValueSchema>(StringComparer.OrdinalIgnoreCase);

  [Test]
  public void Build_CustomCommandWithScalarOutput_ReturnsAssignedVariable()
  {
    var template = CreateCustomCommandTemplate("command-id", "measurement");
    var schemas = CreateSchemaLookup(("COMMAND-ID", new AresValueSchema { Type = AresDataType.Number }));

    var references = CommandOutputVariableReferenceBuilder.Build(template, schemas);

    Assert.That(references, Is.EqualTo(new[]
    {
      new CommandOutputVariableReference("measurement", AresDataType.Number)
    }));
  }

  [Test]
  public void Build_CustomCommandWithNestedStructOutput_ReturnsEveryPath()
  {
    var template = CreateCustomCommandTemplate("command-id", "result");
    var innerStruct = new AresStructSchema();
    innerStruct.Fields["value"] = new AresValueSchema { Type = AresDataType.Number };
    var outerStruct = new AresStructSchema();
    outerStruct.Fields["sample"] = new AresValueSchema
    {
      Type = AresDataType.Struct,
      StructSchema = innerStruct
    };
    var schemas = CreateSchemaLookup(("command-id", new AresValueSchema
    {
      Type = AresDataType.Struct,
      StructSchema = outerStruct
    }));

    var references = CommandOutputVariableReferenceBuilder.Build(template, schemas);

    Assert.That(references, Is.EqualTo(new[]
    {
      new CommandOutputVariableReference("result", AresDataType.Struct),
      new CommandOutputVariableReference("result.sample", AresDataType.Struct),
      new CommandOutputVariableReference("result.sample.value", AresDataType.Number)
    }));
  }

  [Test]
  public void Build_CustomCommandNotInLookup_ReturnsNoReferences()
  {
    var template = CreateCustomCommandTemplate("missing-command", "result");

    var references = CommandOutputVariableReferenceBuilder.Build(template, NoCustomCommandSchemas);

    Assert.That(references, Is.Empty);
  }

  [TestCase(AresDataType.Unit)]
  [TestCase(AresDataType.UnspecifiedType)]
  public void Build_CustomCommandWithoutUsableOutput_ReturnsNoReferences(AresDataType outputType)
  {
    var template = CreateCustomCommandTemplate("command-id", "result");
    var schemas = CreateSchemaLookup(("command-id", new AresValueSchema { Type = outputType }));

    var references = CommandOutputVariableReferenceBuilder.Build(template, schemas);

    Assert.That(references, Is.Empty);
  }

  [Test]
  public void Build_TemplateWithoutOutputVariable_ReturnsNoReferences()
  {
    var template = CreateCustomCommandTemplate("command-id", null);
    var schemas = CreateSchemaLookup(("command-id", new AresValueSchema { Type = AresDataType.Number }));

    var references = CommandOutputVariableReferenceBuilder.Build(template, schemas);

    Assert.That(references, Is.Empty);
  }

  [Test]
  public void Build_DeviceCommand_PreservesExistingBehavior()
  {
    var template = new CommandTemplate
    {
      OutputVarName = "device_result",
      DeviceCommand = new DeviceCommand
      {
        Metadata = new CommandMetadata
        {
          OutputMetadata = new OutputMetadata
          {
            DataSchema = new AresValueSchema { Type = AresDataType.String }
          }
        }
      }
    };

    var references = CommandOutputVariableReferenceBuilder.Build(template, NoCustomCommandSchemas);

    Assert.That(references, Is.EqualTo(new[]
    {
      new CommandOutputVariableReference("device_result", AresDataType.String)
    }));
  }

  [Test]
  public void Build_SystemCommand_PreservesExistingBehavior()
  {
    var template = new CommandTemplate
    {
      OutputVarName = "timestamp",
      SystemCommand = new SystemCommand { Operation = SystemOperation.GetTimestamp }
    };

    var references = CommandOutputVariableReferenceBuilder.Build(template, NoCustomCommandSchemas);

    Assert.That(references, Is.EqualTo(new[]
    {
      new CommandOutputVariableReference("timestamp", AresDataType.Timestamp)
    }));
  }

  private static CommandTemplate CreateCustomCommandTemplate(string customCommandId, string? outputVariableName)
  {
    var template = new CommandTemplate
    {
      CustomCommandInvocation = new CustomCommandInvocation { CustomCommandId = customCommandId }
    };
    if(outputVariableName is not null)
      template.OutputVarName = outputVariableName;

    return template;
  }

  private static IReadOnlyDictionary<string, AresValueSchema> CreateSchemaLookup(
    params (string Id, AresValueSchema Schema)[] schemas)
    => schemas.ToDictionary(schema => schema.Id, schema => schema.Schema, StringComparer.OrdinalIgnoreCase);
}
