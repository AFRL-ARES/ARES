using Ares.Datamodel.Templates;
using Ares.Services;
using UI.Backend.ViewModels.Automation.CampaignEdit;

namespace UI.Features.CampaignEdit.Factories;

public class AnalyzerInputDesignerVmFactory
{
  readonly AresAnalysisService.AresAnalysisServiceClient _analysisServiceClient;
  readonly AresAnalyzerManagementService.AresAnalyzerManagementServiceClient _analyzerManagementClient;
  public AnalyzerInputDesignerVmFactory(AresAnalysisService.AresAnalysisServiceClient analysisServiceClient, AresAnalyzerManagementService.AresAnalyzerManagementServiceClient analyzerManagementClient)
  {
    _analyzerManagementClient = analyzerManagementClient;
    _analysisServiceClient = analysisServiceClient;
  }

  public AnalyzerDesignerViewModel Create(ExperimentTemplate experimentTemplate, IEnumerable<CommandDesignerViewModel> commandDesignerViewModels, IEnumerable<CommandDesignerViewModel> startupCommandDesignerViewModels) => new(_analysisServiceClient, _analyzerManagementClient, experimentTemplate, commandDesignerViewModels, startupCommandDesignerViewModels);
}
