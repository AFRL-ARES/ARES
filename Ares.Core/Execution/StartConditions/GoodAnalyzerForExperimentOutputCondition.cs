using Ares.Core.Analyzing;
using Ares.Core.Validation.Campaign;

namespace Ares.Core.Execution.StartConditions;

internal class GoodAnalyzerForExperimentOutputCondition : IStartCondition
{
  private readonly IActiveCampaignTemplateStore _activeCampaignTemplateStore;
  private readonly IAnalyzerManager _analyzerManager;
  private readonly ICampaignValidator _analyzerValidator;

  public GoodAnalyzerForExperimentOutputCondition(IActiveCampaignTemplateStore activeCampaignTemplateStore, IAnalyzerManager analyzerManager, IEnumerable<ICampaignValidator> validators)
  {
    _activeCampaignTemplateStore = activeCampaignTemplateStore;
    _analyzerManager = analyzerManager;
    _analyzerValidator = validators.OfType<GoodAnalyzerCampaignValidator>().First();
  }

  public StartConditionResult? CanStart()
  {
    if (_activeCampaignTemplateStore.CampaignTemplate?.ExperimentTemplates.All(template => template is null) ?? true)
      return null;

    var validation = _analyzerValidator.Validate(_activeCampaignTemplateStore.CampaignTemplate);

    return new StartConditionResult(validation.Success, validation.Messages);
  }
}
