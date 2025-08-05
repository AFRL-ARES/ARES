using Ares.Core.Execution;
using Ares.Core.Execution.ControlTokens;
using Ares.Core.Execution.Executors;
using Ares.Core.Execution.Executors.Composers;
using Ares.Core.Execution.StartConditions;
using Ares.Core.Execution.StopConditions;
using Ares.Messaging;
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Ares.Core.Tests.Execution;

internal class ExecutionManagerTests
{
  private ICommandComposer<CampaignTemplate, ICampaignExecutor> _campaignComposer;
  private IDbContextFactory<CoreDatabaseContext> _contextFactory;
  private IExecutionReportStore _executionReportStore;

  [OneTimeSetUp]
  public void OneTimeSetUp()
  {
    _executionReportStore = new ExecutionReportStore();
    var mockDbContextFactory = new Mock<IDbContextFactory<CoreDatabaseContext>>();
    mockDbContextFactory.Setup(factory => factory.CreateDbContext()).Returns(new CoreDatabaseContext(new DbContextOptionsBuilder<CoreDatabaseContext>().UseInMemoryDatabase("Ares.Core.Test.Database").Options));
    mockDbContextFactory.Setup(factory => factory.CreateDbContextAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(mockDbContextFactory.Object.CreateDbContext()));
    _contextFactory = mockDbContextFactory.Object;

    var mockCampaignComposer = new Mock<ICommandComposer<CampaignTemplate, ICampaignExecutor>>();
    var mockCampaignExecutor = new Mock<ICampaignExecutor>();
    mockCampaignExecutor.SetupGet(executor => executor.StopConditions).Returns(new List<IStopCondition>());
    mockCampaignExecutor.Setup(executor => executor.Execute(It.IsAny<ExecutionControlToken>())).ReturnsAsync(new CampaignExecutionSummary
    {
      UniqueId = Guid.NewGuid().ToString(),
      CampaignId = Guid.NewGuid().ToString(),
      ExecutionInfo = new ExecutionInfo
      { UniqueId = Guid.NewGuid().ToString(), TimeStarted = DateTime.UtcNow.ToTimestamp(), TimeFinished = DateTime.UtcNow.ToTimestamp() }
    });

    mockCampaignComposer.Setup(composer => composer.Compose(It.IsAny<CampaignTemplate>())).Returns(mockCampaignExecutor.Object);
    _campaignComposer = mockCampaignComposer.Object;
  }

  [Test]
  public void ExecutionManager_Should_Execute_Without_Throwing_Exception()
  {
    var expTemplate = new ExperimentTemplate();
    var campaignTemplate = new CampaignTemplate();
    campaignTemplate.ExperimentTemplates.Add(expTemplate);
    var mockTemplateStore = new Mock<IActiveCampaignTemplateStore>();
    mockTemplateStore.Setup(store => store.CampaignTemplate).Returns(campaignTemplate);
    var executionManager = new ExecutionManager(Array.Empty<IStartCondition>(), _contextFactory, mockTemplateStore.Object, _campaignComposer);
    executionManager.CampaignStopConditions.Add(new NumExperimentsRun(_executionReportStore, 1));
    Assert.DoesNotThrowAsync(() => executionManager.Start(string.Empty, new List<AresCampaignTag>()));
  }

  [Test]
  public void ExecutionManager_Should_Throw_When_CampaignTemplate_Is_Null()
  {
    var mockTemplateStore = new Mock<IActiveCampaignTemplateStore>();
    mockTemplateStore.Setup(store => store.CampaignTemplate).Returns((CampaignTemplate)null);
    var executionManager = new ExecutionManager(Array.Empty<IStartCondition>(), _contextFactory, mockTemplateStore.Object, _campaignComposer);
    Assert.ThrowsAsync<InvalidOperationException>(() => executionManager.Start(string.Empty, new List<AresCampaignTag>()));
  }

  [Test]
  public void ExecutionManager_Should_Throw_When_Start_Condition_Fails()
  {
    var mockTemplateStore = new Mock<IActiveCampaignTemplateStore>();
    mockTemplateStore.Setup(store => store.CampaignTemplate).Returns(new CampaignTemplate());
    var falseCondition = new Mock<IStartCondition>();
    falseCondition.Setup(condition => condition.CanStart()).Returns(Task.FromResult(new StartConditionResult(false)));
    var executionManager = new ExecutionManager(new[] { falseCondition.Object }, _contextFactory, mockTemplateStore.Object, _campaignComposer);
    Assert.ThrowsAsync<InvalidOperationException>(() => executionManager.Start(string.Empty, new List<AresCampaignTag>()));
  }
}
