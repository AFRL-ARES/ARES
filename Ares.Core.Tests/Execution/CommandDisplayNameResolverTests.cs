using Ares.Core.CustomCommands;
using Ares.Core.Execution.Executors;
using Ares.Datamodel.Automation;
using Ares.Datamodel.Templates;
using Microsoft.Extensions.Logging;
using Moq;

namespace Ares.Core.Tests.Execution;

internal class CommandDisplayNameResolverTests
{
  [Test]
  public void Resolve_ReturnsDeviceCommandName()
  {
    var resolver = CreateResolver();
    var template = new CommandTemplate
    {
      DeviceCommand = new DeviceCommand { Metadata = new CommandMetadata { Name = "Dispense" } }
    };

    Assert.That(resolver.Resolve(template), Is.EqualTo("Dispense"));
  }

  [Test]
  public void Resolve_ReturnsSystemOperationName()
  {
    var resolver = CreateResolver();
    var template = new CommandTemplate
    {
      SystemCommand = new SystemCommand { Operation = SystemOperation.SleepForSeconds }
    };

    Assert.That(resolver.Resolve(template), Is.EqualTo(nameof(SystemOperation.SleepForSeconds)));
  }

  [Test]
  public async Task Resolve_ReturnsCurrentCustomCommandName_CaseInsensitively()
  {
    var persistence = new Mock<ICustomCommandPersistenceService>();
    persistence.Setup(service => service.GetCommandsAsync()).ReturnsAsync([
      new CustomCommandVersion { CustomCommandId = "COMMAND-ID", Name = "Measure Temperature" }
    ]);
    var resolver = CreateResolver(persistence.Object);
    await resolver.RefreshAsync();

    Assert.That(resolver.Resolve(CreateCustomTemplate("command-id")), Is.EqualTo("Measure Temperature"));
  }

  [TestCase(null)]
  [TestCase("")]
  [TestCase("blank-name")]
  [TestCase("missing-id")]
  public async Task Resolve_ReturnsGenericName_WhenCustomCommandCannotBeNamed(string customCommandId)
  {
    var persistence = new Mock<ICustomCommandPersistenceService>();
    persistence.Setup(service => service.GetCommandsAsync()).ReturnsAsync([
      new CustomCommandVersion { CustomCommandId = "blank-name", Name = "  " }
    ]);
    var resolver = CreateResolver(persistence.Object);
    await resolver.RefreshAsync();

    Assert.That(resolver.Resolve(CreateCustomTemplate(customCommandId)), Is.EqualTo("Custom Command"));
  }

  [Test]
  public async Task RefreshAsync_UsesGenericNames_WhenPersistenceFails()
  {
    var persistence = new Mock<ICustomCommandPersistenceService>();
    persistence.Setup(service => service.GetCommandsAsync()).ThrowsAsync(new InvalidOperationException("Database unavailable"));
    var resolver = CreateResolver(persistence.Object);

    Assert.DoesNotThrowAsync(resolver.RefreshAsync);
    Assert.That(resolver.Resolve(CreateCustomTemplate("command-id")), Is.EqualTo("Custom Command"));
  }

  private static CommandDisplayNameResolver CreateResolver(ICustomCommandPersistenceService persistence = null)
    => new(
      persistence ?? new Mock<ICustomCommandPersistenceService>().Object,
      new Mock<ILogger<CommandDisplayNameResolver>>().Object);

  private static CommandTemplate CreateCustomTemplate(string customCommandId)
    => new()
    {
      CustomCommandInvocation = new CustomCommandInvocation { CustomCommandId = customCommandId ?? string.Empty }
    };
}
