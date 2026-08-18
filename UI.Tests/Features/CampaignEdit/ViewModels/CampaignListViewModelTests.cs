using Ares.Core.Campaigns;
using Ares.Datamodel.Templates;
using Microsoft.AspNetCore.Components.Forms;
using Moq;
using System.Text;
using UI.Features.CampaignEdit.ViewModels;

namespace UI.Tests.Features.CampaignEdit.ViewModels;

internal class CampaignListViewModelTests
{
  [Test]
  public void ImportCampaignTemplate_RejectsFilesOverFiveMiB()
  {
    var file = new Mock<IBrowserFile>();
    file.SetupGet(value => value.Size).Returns(CampaignListViewModel.MaximumImportFileSize + 1);
    var viewModel = new CampaignListViewModel(null!, Mock.Of<ICampaignTemplateTransferService>());

    Assert.ThrowsAsync<CampaignTemplateImportException>(() => viewModel.ImportCampaignTemplate(file.Object));
  }

  [Test]
  public async Task ImportCampaignTemplate_PassesFileContentsToTransferService()
  {
    const string json = "{\"name\":\"Campaign\"}";
    var file = new Mock<IBrowserFile>();
    file.SetupGet(value => value.Size).Returns(Encoding.UTF8.GetByteCount(json));
    file.Setup(value => value.OpenReadStream(CampaignListViewModel.MaximumImportFileSize, It.IsAny<CancellationToken>()))
      .Returns(new MemoryStream(Encoding.UTF8.GetBytes(json)));
    var transfer = new Mock<ICampaignTemplateTransferService>();
    transfer.Setup(service => service.ImportAsync(json, It.IsAny<CancellationToken>()))
      .ReturnsAsync(new CampaignTemplateImportResult(new CampaignTemplate { Name = "Campaign" }, []));
    var viewModel = new CampaignListViewModel(null!, transfer.Object);

    var result = await viewModel.ImportCampaignTemplate(file.Object);

    Assert.That(result.Template.Name, Is.EqualTo("Campaign"));
    transfer.Verify(service => service.ImportAsync(json, It.IsAny<CancellationToken>()), Times.Once);
  }
}
