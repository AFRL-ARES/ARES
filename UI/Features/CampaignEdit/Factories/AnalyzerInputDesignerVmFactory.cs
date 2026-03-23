using Ares.Datamodel.Templates;
using Ares.Core.Grpc.Services;
using AnalyzerDesignerViewModel =UI.Features.CampaignEdit.ViewModels.AnalyzerDesignerViewModel;
using CommandDesignerViewModel=UI.Features.CampaignEdit.ViewModels.CommandDesignerViewModel;

namespace UI.Features.CampaignEdit.Factories;

public class AnalyzerInputDesignerVmFactory
{
  readonly AnalysisService _analysisService;
  readonly AnalyzerService _analyzerManagementClient;

  public AnalyzerInputDesignerVmFactory(AnalysisService analysisService, AnalyzerService analyzerManagementClient)
  {
    _analyzerManagementClient = analyzerManagementClient;
    _analysisService = analysisService;
  }

  public AnalyzerDesignerViewModel Create(ExperimentTemplate experimentTemplate, IEnumerable<CommandDesignerViewModel> commandDesignerViewModels, IEnumerable<CommandDesignerViewModel> startupCommandDesignerViewModels) => new(_analysisService, _analyzerManagementClient, experimentTemplate, commandDesignerViewModels, startupCommandDesignerViewModels);
}
