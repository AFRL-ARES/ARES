using Ares.Core.CustomCommands;
using Ares.Core.Execution.Executors;
using Ares.Core.Scripting;
using Ares.Datamodel;
using Ares.Datamodel.Automation;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Factories;
using Ares.Datamodel.Templates;
using Moq;

namespace Ares.Core.Tests.Execution;

[TestFixture]
internal class CustomCommandExecutorTests
{
  [Test]
  public async Task Execute_ReturnsWrappedCustomCommandResult()
  {
    var commandId = Guid.NewGuid();
    var command = new CustomCommandVersion
    {
      Name = "Increment",
      OutputSchema = AresSchemaBuilder.Entry(AresDataType.Number).Build(),
      ScriptBody = "return value + 1"
    };
    command.InputParameters.Add(new CustomCommandParameter
    {
      Name = "value",
      Schema = AresSchemaBuilder.Entry(AresDataType.Number).Build()
    });

    var persistenceService = new Mock<ICustomCommandPersistenceService>();
    persistenceService
      .Setup(service => service.GetAsync(commandId))
      .ReturnsAsync(command);
    var executor = new CustomCommandExecutor(
      persistenceService.Object,
      new BaseEnvironmentBuilder([]));
    var binding = new Parameter
    {
      Metadata = new ParameterMetadata { Name = "value" },
      LiteralSource = new LiteralParameterSource { Value = AresValueHelper.CreateNumber(41) }
    };

    var result = await executor.Execute(commandId.ToString(), [binding], CancellationToken.None);

    using(Assert.EnterMultipleScope())
    {
      Assert.That(result.Success, Is.True, result.Error);
      Assert.That(result.Result?.NumberValue, Is.EqualTo(42));
    }
  }
}
