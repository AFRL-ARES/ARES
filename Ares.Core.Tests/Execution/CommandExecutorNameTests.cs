using Ares.Core.Execution.ControlTokens;
using Ares.Core.Execution.Executors;
using Ares.Core.Notifications;
using Ares.Core.Settings;
using Ares.Datamodel;
using Ares.Datamodel.Templates;
using Moq;

namespace Ares.Core.Tests.Execution;

internal class CommandExecutorNameTests
{
  [Test]
  public async Task SuppliedName_IsUsedForInitialStatusAndCompletedSummary()
  {
    var executor = new CommandExecutor(
      _ => Task.FromResult(new CommandResult { Success = true }),
      new CommandTemplate
      {
        UniqueId = Guid.NewGuid().ToString(),
        CustomCommandInvocation = new CustomCommandInvocation { CustomCommandId = Guid.NewGuid().ToString() }
      },
      "Measure Temperature",
      new Mock<INotifier>().Object,
      new Mock<ISystemSettingsManager>().Object);

    Assert.That(executor.Status.CommandName, Is.EqualTo("Measure Temperature"));

    using var tokenSource = new ExecutionControlTokenSource();
    var summary = await executor.Execute(tokenSource.Token);

    Assert.That(summary.CommandName, Is.EqualTo("Measure Temperature"));
  }
}
