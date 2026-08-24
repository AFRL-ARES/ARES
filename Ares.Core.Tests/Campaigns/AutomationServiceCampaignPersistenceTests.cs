using Ares.Core.Analyzing;
using Ares.Core.Campaigns;
using Ares.Core.Execution;
using Ares.Core.Execution.Extensions;
using Ares.Core.Execution.StartConditions;
using Ares.Core.Execution.StopConditions;
using Ares.Core.Notifications;
using Ares.Core.Planning;
using Ares.Core.Grpc.Services;
using Ares.Datamodel.Templates;
using Ares.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Ares.Core.Execution.StopConditions.PlannerLead;

namespace Ares.Core.Tests.Campaigns;

internal class AutomationServiceCampaignPersistenceTests
{
  [Test]
  public async Task GetAllCampaigns_UsesDatabasePersistence()
  {
    var persistence = new Mock<ICampaignTemplatePersistenceService>();
    persistence.Setup(service => service.GetSummariesAsync(It.IsAny<CancellationToken>()))
      .ReturnsAsync([new CampaignTemplateSummary { UniqueId = "campaign-id", CampaignName = "Campaign" }]);
    var service = CreateService(persistence.Object, new ActiveCampaignTemplateStore());

    var response = await service.GetAllCampaigns(new GetAllCampaignsRequest(), null);

    Assert.That(response.Campaigns.Single().CampaignName, Is.EqualTo("Campaign"));
    persistence.Verify(service => service.GetSummariesAsync(It.IsAny<CancellationToken>()), Times.Once);
  }

  [Test]
  public async Task RemoveCampaign_DoesNotDeleteActiveCampaign()
  {
    var persistence = new Mock<ICampaignTemplatePersistenceService>();
    var activeStore = new ActiveCampaignTemplateStore
    {
      CampaignTemplate = new CampaignTemplate { UniqueId = "active-id", Name = "Active" }
    };
    var service = CreateService(persistence.Object, activeStore);

    await service.RemoveCampaign(new CampaignRequest { UniqueId = "active-id" }, null);

    persistence.Verify(value => value.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
  }

  [Test]
  public async Task SetCampaignForExecution_LoadsDatabaseCampaign()
  {
    var campaign = new CampaignTemplate { UniqueId = "campaign-id", Name = "Campaign" };
    var persistence = new Mock<ICampaignTemplatePersistenceService>();
    persistence.Setup(service => service.GetByIdAsync("campaign-id", It.IsAny<CancellationToken>())).ReturnsAsync(campaign);
    var activeStore = new ActiveCampaignTemplateStore();
    var service = CreateService(persistence.Object, activeStore);

    var result = await service.SetCampaignForExecution(new CampaignRequest { UniqueId = "campaign-id" }, null);

    Assert.That(result, Is.SameAs(campaign));
    Assert.That(activeStore.CampaignTemplate, Is.SameAs(campaign));
  }

  [Test]
  public async Task GetCopyOfCampaign_ExportsDatabaseCampaignAsJson()
  {
    var campaign = new CampaignTemplate { UniqueId = Guid.NewGuid().ToString(), Name = "Campaign" };
    var persistence = new Mock<ICampaignTemplatePersistenceService>();
    var transfer = new Mock<ICampaignTemplateTransferService>();
    transfer.Setup(service => service.ExportAsync(campaign.UniqueId, It.IsAny<CancellationToken>()))
      .ReturnsAsync(new CampaignTemplateExport(campaign, "{\"uniqueId\":\"campaign-id\"}", "Campaign.json"));
    var service = CreateService(persistence.Object, new ActiveCampaignTemplateStore(), transfer.Object);

    var response = await service.GetCopyOfCampaign(new CampaignRequest { UniqueId = campaign.UniqueId }, null!);

    using(Assert.EnterMultipleScope())
    {
      Assert.That(response.Template, Is.SameAs(campaign));
      Assert.That(response.SerializedJsonData, Does.Contain("campaign-id"));
    }
  }

  private static AutomationService CreateService(
    ICampaignTemplatePersistenceService persistence,
    IActiveCampaignTemplateStore activeStore,
    ICampaignTemplateTransferService transferService = null)
    => new(
      Mock.Of<IDbContextFactory<CoreDatabaseContext>>(),
      Mock.Of<IExecutionManager>(),
      Mock.Of<IExecutionReportStore>(),
      activeStore,
      [],
      [],
      Mock.Of<IDesiredAnalysisResultFactory>(),
      Mock.Of<IPlannerLeadStopConditionFactory>(),
      Mock.Of<IPlannerServiceRepo>(),
      Mock.Of<IPlannerTransactionProvider>(),
      Mock.Of<IAnalyzerTransactionProvider>(),
      persistence,
      transferService ?? Mock.Of<ICampaignTemplateTransferService>());
}
