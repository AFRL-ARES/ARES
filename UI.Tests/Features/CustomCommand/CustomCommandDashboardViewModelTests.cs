using System.Reactive.Subjects;
using Ares.Core.CustomCommands;
using Ares.Core.Execution;
using Ares.Core.Execution.Executors;
using Ares.Core.Scripting;
using Ares.Datamodel;
using Ares.Datamodel.Automation;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Factories;
using Microsoft.Extensions.Logging;
using Moq;
using UI.Application.Notifications;
using UI.Features.CustomCommand;

namespace UI.Tests.Features.CustomCommand;

public class CustomCommandDashboardViewModelTests
{
  [Test]
  public async Task InitializeAsync_LoadsCommandsInNameOrder()
  {
    var first = CreateCommand("Zulu", "return 1");
    var second = CreateCommand("Alpha", "return 2");
    using var fixture = CreateFixture([first, second]);

    await fixture.ViewModel.InitializeAsync();

    Assert.That(fixture.ViewModel.AvailableCommands.Select(command => command.Name), Is.EqualTo(["Alpha", "Zulu"]));
  }

  [Test]
  public async Task RunAsync_ExecutesSelectedCommandWithEditedArguments()
  {
    var command = CreateCommand("Increment", "return value + 1");
    command.InputParameters.Add(new CustomCommandParameter
    {
      Name = "value",
      Schema = AresSchemaBuilder.Entry(AresDataType.Number).Build()
    });
    using var fixture = CreateFixture([command]);
    await fixture.ViewModel.InitializeAsync();
    fixture.ViewModel.SelectCommand(command.CustomCommandId);
    fixture.ViewModel.UpdateArgument("value", AresValueHelper.CreateNumber(41));

    await fixture.ViewModel.RunAsync();

    using(Assert.EnterMultipleScope())
    {
      Assert.That(fixture.ViewModel.Result?.Success, Is.True);
      Assert.That(fixture.ViewModel.Result?.Result?.NumberValue, Is.EqualTo(42));
      Assert.That(fixture.ViewModel.ExecutionError, Is.Null);
      Assert.That(fixture.ViewModel.ExecutionDuration, Is.Not.Null);
    }
  }

  [Test]
  public async Task RunAsync_DoesNotExecuteWhenCampaignBecomesActive()
  {
    var command = CreateCommand("Measure", "return 1");
    using var fixture = CreateFixture([command]);
    await fixture.ViewModel.InitializeAsync();
    fixture.ViewModel.SelectCommand(command.CustomCommandId);

    fixture.SetCampaignState(ExecutionState.Running);
    await fixture.ViewModel.RunAsync();

    using(Assert.EnterMultipleScope())
    {
      Assert.That(fixture.ViewModel.IsCampaignActive, Is.True);
      Assert.That(fixture.ViewModel.CanRun, Is.False);
      Assert.That(fixture.ViewModel.Result, Is.Null);
    }
    fixture.Persistence.Verify(service => service.GetAsync(It.IsAny<Guid>()), Times.Never);
  }

  [TestCase(ExecutionState.Undefined, false)]
  [TestCase(ExecutionState.Succeeded, false)]
  [TestCase(ExecutionState.Failed, false)]
  [TestCase(ExecutionState.Waiting, true)]
  [TestCase(ExecutionState.AwaitingUser, true)]
  [TestCase(ExecutionState.Running, true)]
  [TestCase(ExecutionState.Paused, true)]
  [TestCase(ExecutionState.WaitingForUserDecision, true)]
  [TestCase(ExecutionState.Replanning, true)]
  [TestCase(ExecutionState.Retrying, true)]
  [TestCase(ExecutionState.EnteringSafeMode, true)]
  [TestCase(ExecutionState.Planning, true)]
  [TestCase(ExecutionState.Analyzing, true)]
  [TestCase(ExecutionState.GenerateExecutor, true)]
  [TestCase(ExecutionState.InitializeExperiment, true)]
  public void CampaignStatusUpdate_ControlsExecutionAvailability(ExecutionState state, bool expectedActive)
  {
    using var fixture = CreateFixture([]);

    fixture.SetCampaignState(state);

    Assert.That(fixture.ViewModel.IsCampaignActive, Is.EqualTo(expectedActive));
  }

  private static CustomCommandVersion CreateCommand(string name, string scriptBody)
  {
    return new CustomCommandVersion
    {
      CustomCommandId = Guid.NewGuid().ToString(),
      Name = name,
      ScriptBody = scriptBody,
      OutputSchema = AresSchemaBuilder.Entry(AresDataType.Number).Build()
    };
  }

  private static ViewModelFixture CreateFixture(IReadOnlyList<CustomCommandVersion> commands)
  {
    var persistence = new Mock<ICustomCommandPersistenceService>();
    persistence.Setup(service => service.GetCommandsAsync()).ReturnsAsync(commands);
    foreach(var command in commands)
    {
      persistence.Setup(service => service.GetAsync(Guid.Parse(command.CustomCommandId)))
        .ReturnsAsync(command);
    }

    var statuses = new BehaviorSubject<CampaignExecutionStatus?>(null);
    CampaignExecutionStatus? currentStatus = null;
    var reportStore = new Mock<IExecutionReportStore>();
    reportStore.SetupGet(store => store.CampaignStatusObservable).Returns(statuses);
    reportStore.SetupGet(store => store.CampaignExecutionStatus).Returns(() => currentStatus);
    var executor = new CustomCommandExecutor(persistence.Object, new BaseEnvironmentBuilder([]));
    var notifService = new Mock<IAresUINotificationService>().Object;
    var logger = new Mock<Logger<CustomCommandDashboardViewModel>>().Object;
    var viewModel = new CustomCommandDashboardViewModel(persistence.Object, executor, reportStore.Object, notifService, logger);

    return new ViewModelFixture(viewModel, persistence, statuses, status => currentStatus = status);
  }

  private sealed class ViewModelFixture(
    CustomCommandDashboardViewModel viewModel,
    Mock<ICustomCommandPersistenceService> persistence,
    BehaviorSubject<CampaignExecutionStatus?> statuses,
    Action<CampaignExecutionStatus?> setCurrentStatus) : IDisposable
  {
    public CustomCommandDashboardViewModel ViewModel { get; } = viewModel;
    public Mock<ICustomCommandPersistenceService> Persistence { get; } = persistence;

    public void SetCampaignState(ExecutionState state)
    {
      var status = new CampaignExecutionStatus { State = state };
      setCurrentStatus(status);
      statuses.OnNext(status);
    }

    public void Dispose()
    {
      ViewModel.Dispose();
      statuses.Dispose();
    }
  }
}
