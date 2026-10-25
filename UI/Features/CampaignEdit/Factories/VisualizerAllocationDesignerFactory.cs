using Ares.Core.Analyzing;
using Ares.Core.Visualization.Providers;
using Ares.Datamodel.Templates;
using UI.Features.CampaignEdit.ViewModels;

namespace UI.Features.CampaignEdit.Factories;

public sealed class VisualizerAllocationDesignerFactory(IVisualizerProvider visualizerProvider, IAnalyzerRepo analyzerRepo)
{
  private readonly IVisualizerProvider _visualizerProvider = visualizerProvider;
  private readonly IAnalyzerRepo _analyzerRepo = analyzerRepo;

  public VisualizerAllocationDesignerViewModel Create(
    ExperimentTemplate experimentTemplate,
    IEnumerable<CommandDesignerViewModel> commandDesigners,
    IEnumerable<CommandDesignerViewModel> startupCommandDesigners,
    Func<string?> analyzerIdProvider)
    => new(
      experimentTemplate,
      commandDesigners,
      startupCommandDesigners,
      analyzerIdProvider,
      _analyzerRepo,
      _visualizerProvider);
}