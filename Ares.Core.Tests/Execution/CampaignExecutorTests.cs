using Ares.Core.Analyzing;
using Ares.Core.AresEnvironment;
using Ares.Core.Device.Providers;
using Ares.Core.Device.Repos;
using Ares.Core.Device.State.Logging;
using Ares.Core.Execution;
using Ares.Core.Execution.ControlTokens;
using Ares.Core.Execution.Executors;
using Ares.Core.Execution.Executors.Composers;
using Ares.Core.Execution.Safety;
using Ares.Core.Execution.StopConditions;
using Ares.Core.Notifications;
using Ares.Core.Planning;
using Ares.Core.Settings;
using Ares.Core.Tests.Data;
using Ares.Core.Tests.Data.Analyzer;
using Ares.Core.Tests.Data.Device;
using Ares.Datamodel;
using Ares.Datamodel.Device;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Planning;
using Ares.Datamodel.Templates;
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Reactive.Linq;

namespace Ares.Core.Tests.Execution;

internal class CampaignExecutorTests
{
  private AnalyzerRepo _analyzerRepo;
  private IAnalyzer _replyAnalyzer;
  private AresDeviceRepo _deviceRepo;
  private IExecutionReportStore _executionReportStore;
  private Mock<IPlanningHelper> _planningHelper;
  private Mock<ISystemSettingsManager> _settingsManager;
  private Mock<IExecutionSafetyManager> _safetyManager;
  private CampaignComposer _campaignComposer;

  [SetUp]
  public void SetUp()
  {
    var notifier = Mock.Of<INotifier>();
    _analyzerRepo = new AnalyzerRepo();
    _replyAnalyzer = new TestReplyAnalyzer();
    _analyzerRepo.AddAnalyzer(_replyAnalyzer);

    _executionReportStore = new ExecutionReportStore();
    var executionReporter = new ExecutionReporter(_executionReportStore);

    _planningHelper = new Mock<IPlanningHelper>();
    _planningHelper.Setup(helper => helper.ReseedManualPlanner()).Returns(Task.CompletedTask);
    _planningHelper
      .Setup(helper => helper.TryResolveParameters(
        It.IsAny<IEnumerable<PlannerAllocation>>(),
        It.IsAny<RequestMetadata>(),
        It.IsAny<ExperimentTemplate>(),
        It.IsAny<IEnumerable<Datamodel.Analyzing.AnalysisResponse>>(),
        It.IsAny<IEnumerable<ExperimentOverview>>(),
        It.IsAny<int>(),
        It.IsAny<List<PlanStatusCode>>(),
        It.IsAny<CancellationToken>()))
      .ReturnsAsync(true);

    _settingsManager = new Mock<ISystemSettingsManager>();
    _settingsManager.Setup(manager => manager.GetAresGeneralSettings()).ReturnsAsync(new AresGeneralSettingsConfig
    {
      ExperimentRetryLimit = 1,
      RetryCooldown = new Duration()
    });
    _settingsManager.Setup(manager => manager.GetErrorHandlingByStatusCode(It.IsAny<CommandStatusCode>()))
      .ReturnsAsync(ErrorHandling.StopAndCloseout);

    _safetyManager = new Mock<IExecutionSafetyManager>();
    _safetyManager.Setup(manager => manager.EnterSafeMode()).ReturnsAsync(true);

    var dbOptions = new DbContextOptionsBuilder<CoreDatabaseContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;
    var dbContextFactory = new Mock<IDbContextFactory<CoreDatabaseContext>>();
    dbContextFactory.Setup(factory => factory.CreateDbContext()).Returns(() => new CoreDatabaseContext(dbOptions));
    var analysisHelper = new AnalysisHelper(
      _analyzerRepo,
      Mock.Of<ILogger<AnalysisHelper>>(),
      dbContextFactory.Object,
      notifier);

    _deviceRepo = new AresDeviceRepo();
    _deviceRepo.AddOrUpdate(new ResultSequenceDevice([]));
    var commandDisplayNameResolver = new Mock<ICommandDisplayNameResolver>();
    commandDisplayNameResolver.Setup(value => value.Resolve(It.IsAny<CommandTemplate>())).Returns("Test command");
    var stepComposer = new StepComposer(_deviceRepo, notifier, _settingsManager.Object, commandDisplayNameResolver.Object);
    var experimentComposer = new ExperimentComposer(stepComposer, _analyzerRepo);

    var campaignLogger = new Mock<ILogger<CampaignExecutor>>();
    var loggerFactory = new Mock<ILoggerFactory>();
    loggerFactory.Setup(factory => factory.CreateLogger(typeof(CampaignExecutor).FullName))
      .Returns(campaignLogger.Object);

    var stateLoggerManager = new StateLoggerManager(
      new DeviceStateLoggerRepository(),
      Mock.Of<IDeviceStateLoggerFactory>(),
      Mock.Of<ILogger<StateLoggerManager>>(),
      dbContextFactory.Object,
      Mock.Of<IAresDeviceProvider>());

    _campaignComposer = new CampaignComposer(
      analysisHelper,
      experimentComposer,
      _planningHelper.Object,
      executionReporter,
      [],
      [],
      [],
      _analyzerRepo,
      notifier,
      loggerFactory.Object,
      Mock.Of<AresVariableManager>(),
      stateLoggerManager,
      _settingsManager.Object,
      _safetyManager.Object);
  }

  [TearDown]
  public void TearDown()
  {
    _deviceRepo.Dispose();
  }

  [Test]
  public async Task Successful_Campaign_Reports_Succeeded_Without_Internal_Execution_States()
  {
    var executor = CreateExecutor();
    var states = ObserveCampaignStates();

    var summary = await executor.Execute(new ExecutionControlTokenSource().Token);

    Assert.Multiple(() =>
    {
      Assert.That(executor.Status.State, Is.EqualTo(ExecutionState.Succeeded));
      Assert.That(summary.ExperimentSummaries, Has.Count.EqualTo(1));
      Assert.That(states, Does.Not.Contain(ExecutionState.InitializeExperiment));
      Assert.That(states, Does.Not.Contain(ExecutionState.Planning));
      Assert.That(states, Does.Not.Contain(ExecutionState.GenerateExecutor));
      Assert.That(states, Does.Not.Contain(ExecutionState.Analyzing));
    });
  }

  [Test]
  public async Task Empty_Experiment_Fails_Without_Analysis()
  {
    var emptyExperiment = TestCampaignProvider.GetExperimentTemplate(_replyAnalyzer, "Empty Experiment", "");
    var executor = CreateExecutor(TestCampaignProvider.GetCampaignTemplate("Empty Campaign", emptyExperiment), addStopCondition: false);

    var summary = await executor.Execute(new ExecutionControlTokenSource().Token);

    Assert.Multiple(() =>
    {
      Assert.That(executor.Status.State, Is.EqualTo(ExecutionState.Failed));
      Assert.That(summary.ExperimentSummaries, Is.Empty);
      Assert.That(_executionReportStore.CampaignExecutionStatus?.AnalysisState, Is.Not.EqualTo(AnalysisState.AnalysisInProgress));
    });
  }

  [Test]
  public async Task Failed_Experiment_Retries_And_Can_Succeed()
  {
    UseDeviceResults(FailedResult(), SuccessfulResults(4));
    ConfigureErrorHandling(ErrorHandling.RetryExperiment);
    var executor = CreateExecutor();
    var states = ObserveCampaignStates();

    var summary = await executor.Execute(new ExecutionControlTokenSource().Token);

    Assert.Multiple(() =>
    {
      Assert.That(executor.Status.State, Is.EqualTo(ExecutionState.Succeeded));
      Assert.That(summary.ExperimentSummaries, Has.Count.EqualTo(1));
      Assert.That(states, Does.Contain(ExecutionState.Retrying));
    });
  }

  [Test]
  public async Task Retry_Count_Resets_For_Each_New_Experiment()
  {
    UseDeviceResults(
      FailedResult(),
      SuccessfulResults(4),
      FailedResult(),
      SuccessfulResults(4));
    ConfigureErrorHandling(ErrorHandling.RetryExperiment);
    var executor = CreateExecutor(stopAfterExperiments: 2);

    var summary = await executor.Execute(new ExecutionControlTokenSource().Token);

    Assert.Multiple(() =>
    {
      Assert.That(executor.Status.State, Is.EqualTo(ExecutionState.Succeeded));
      Assert.That(summary.ExperimentSummaries, Has.Count.EqualTo(2));
    });
  }

  [Test]
  public async Task Replan_Composes_And_Executes_Experiment_Again()
  {
    UseDeviceResults(FailedResult(), SuccessfulResults(4));
    ConfigureErrorHandling(ErrorHandling.Replan);
    var executor = CreateExecutor();
    var states = ObserveCampaignStates();

    await executor.Execute(new ExecutionControlTokenSource().Token);

    Assert.Multiple(() =>
    {
      Assert.That(executor.Status.State, Is.EqualTo(ExecutionState.Succeeded));
      Assert.That(states, Does.Contain(ExecutionState.Replanning));
    });
    _planningHelper.Verify(helper => helper.TryResolveParameters(
      It.IsAny<IEnumerable<PlannerAllocation>>(),
      It.IsAny<RequestMetadata>(),
      It.IsAny<ExperimentTemplate>(),
      It.IsAny<IEnumerable<Datamodel.Analyzing.AnalysisResponse>>(),
      It.IsAny<IEnumerable<ExperimentOverview>>(),
      It.IsAny<int>(),
      It.IsAny<List<PlanStatusCode>>(),
      It.IsAny<CancellationToken>()), Times.Once);
  }

  [Test]
  public async Task Safe_Mode_Is_Entered_Exactly_Once()
  {
    UseDeviceResults(FailedResult());
    ConfigureErrorHandling(ErrorHandling.EnterSafeMode);
    var executor = CreateExecutor(addStopCondition: false);

    await executor.Execute(new ExecutionControlTokenSource().Token);

    Assert.That(executor.Status.State, Is.EqualTo(ExecutionState.Failed));
    _safetyManager.Verify(manager => manager.EnterSafeMode(), Times.Once);
  }

  [Test]
  public async Task Prompted_Error_Reports_Waiting_And_Follows_Decision()
  {
    UseDeviceResults(FailedResult(), SuccessfulResults(4));
    ConfigureErrorHandling(ErrorHandling.PromptUser);
    var executor = CreateExecutor();
    var waitingForDecision = ObserveState(ExecutionState.WaitingForUserDecision);
    var states = ObserveCampaignStates();

    var execution = executor.Execute(new ExecutionControlTokenSource().Token);
    await waitingForDecision.Task.WaitAsync(TimeSpan.FromSeconds(5));
    executor.SubmitUserDecision(ErrorHandling.RetryExperiment);
    await execution;

    Assert.Multiple(() =>
    {
      Assert.That(executor.Status.State, Is.EqualTo(ExecutionState.Succeeded));
      Assert.That(states, Does.Contain(ExecutionState.WaitingForUserDecision));
      Assert.That(states, Does.Contain(ExecutionState.Retrying));
    });
  }

  [Test]
  public async Task Stop_While_Waiting_For_Decision_Completes_And_Reports_Failed()
  {
    UseDeviceResults(FailedResult());
    ConfigureErrorHandling(ErrorHandling.PromptUser);
    var executor = CreateExecutor(addStopCondition: false);
    var controlTokenSource = new ExecutionControlTokenSource();
    var waitingForDecision = ObserveState(ExecutionState.WaitingForUserDecision);

    var execution = executor.Execute(controlTokenSource.Token);
    await waitingForDecision.Task.WaitAsync(TimeSpan.FromSeconds(5));
    controlTokenSource.Cancel();
    var summary = await execution.WaitAsync(TimeSpan.FromSeconds(5));

    Assert.Multiple(() =>
    {
      Assert.That(executor.Status.State, Is.EqualTo(ExecutionState.Failed));
      Assert.That(summary.CloseoutExecutionSummary, Is.Not.Null);
    });
  }

  [Test]
  public async Task Stop_During_Retry_Cooldown_Completes_And_Reports_Failed()
  {
    UseDeviceResults(FailedResult());
    ConfigureErrorHandling(ErrorHandling.RetryExperiment);
    _settingsManager.Setup(manager => manager.GetAresGeneralSettings()).ReturnsAsync(new AresGeneralSettingsConfig
    {
      ExperimentRetryLimit = 1,
      RetryCooldown = Duration.FromTimeSpan(TimeSpan.FromMinutes(1))
    });
    var executor = CreateExecutor(addStopCondition: false);
    var controlTokenSource = new ExecutionControlTokenSource();
    var retrying = ObserveState(ExecutionState.Retrying);

    var execution = executor.Execute(controlTokenSource.Token);
    await retrying.Task.WaitAsync(TimeSpan.FromSeconds(5));
    controlTokenSource.Cancel();
    var summary = await execution.WaitAsync(TimeSpan.FromSeconds(5));

    Assert.Multiple(() =>
    {
      Assert.That(executor.Status.State, Is.EqualTo(ExecutionState.Failed));
      Assert.That(summary.CloseoutExecutionSummary, Is.Not.Null);
    });
  }

  private ICampaignExecutor CreateExecutor(CampaignTemplate template = null, int stopAfterExperiments = 1, bool addStopCondition = true)
  {
    template ??= TestCampaignProvider.GetSampleCampaignTemplate(_replyAnalyzer);
    template.StartupTemplate ??= CreateEmptyTemplate("Startup");
    template.CloseoutTemplate ??= CreateEmptyTemplate("Closeout");
    var commands = template.ExperimentTemplate.StepTemplates.SelectMany(step => step.CommandTemplates).ToArray();
    for(var i = 1; i < commands.Length; i++)
      commands[i].OutputVarName = $"UnusedOutput{i}";

    var executor = _campaignComposer.Compose(template);
    if(addStopCondition)
      executor.StopConditions.Add(new NumExperimentsRun(_executionReportStore, (uint)stopAfterExperiments));

    return executor;
  }

  private ExperimentTemplate CreateEmptyTemplate(string name)
    => new()
    {
      AnalyzerId = _replyAnalyzer.UniqueId,
      Name = name,
      Resolved = true,
      UniqueId = Guid.NewGuid().ToString()
    };

  private List<ExecutionState> ObserveCampaignStates()
  {
    var states = new List<ExecutionState>();
    _executionReportStore.CampaignStatusObservable
      .Where(status => status is not null)
      .Select(status => status!.State)
      .Subscribe(states.Add);
    return states;
  }

  private TaskCompletionSource ObserveState(ExecutionState expectedState)
  {
    var source = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    _executionReportStore.CampaignStatusObservable
      .Where(status => status?.State == expectedState)
      .Take(1)
      .Subscribe(_ => source.TrySetResult());
    return source;
  }

  private void ConfigureErrorHandling(ErrorHandling handling)
  {
    _settingsManager.Setup(manager => manager.GetErrorHandlingByStatusCode(It.IsAny<CommandStatusCode>()))
      .ReturnsAsync(handling);
  }

  private void UseDeviceResults(params CommandResult[][] resultGroups)
  {
    _deviceRepo.AddOrUpdate(new ResultSequenceDevice(resultGroups.SelectMany(results => results)));
  }

  private static CommandResult[] SuccessfulResults(int count)
    => Enumerable.Range(0, count).Select(_ => SuccessfulResult()).ToArray();

  private static CommandResult SuccessfulResult()
    => new() { Success = true, Result = AresValueHelper.CreateNumber(1) };

  private static CommandResult[] FailedResult()
    => [new CommandResult { Success = false, StatusCode = CommandStatusCode.CommandFailed, Error = "Test failure" }];

  private sealed class ResultSequenceDevice : TestDevice
  {
    private readonly Queue<CommandResult> _results;

    public ResultSequenceDevice(IEnumerable<CommandResult> results)
    {
      _results = new Queue<CommandResult>(results);
    }

    public override Task<CommandResult> ExecuteCommand(string command, List<DeviceCommandArgument> parameters, CancellationToken token)
    {
      if(token.IsCancellationRequested)
        return Task.FromResult(new CommandResult { Success = false });

      return Task.FromResult(_results.Count > 0 ? _results.Dequeue() : SuccessfulResult());
    }
  }
}
