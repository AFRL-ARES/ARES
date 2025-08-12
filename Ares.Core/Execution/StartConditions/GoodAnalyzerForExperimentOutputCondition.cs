using Ares.Core.Analyzing;
using Ares.Core.Validation.Campaign;

namespace Ares.Core.Execution.StartConditions;

internal class GoodAnalyzerForExperimentOutputCondition : IStartCondition
{
  private readonly IActiveCampaignTemplateStore _activeCampaignTemplateStore;
  private readonly IAnalyzerRepo _analyzerManager;
  private readonly ICampaignValidator _campaignAnalyzerValidator;

  public GoodAnalyzerForExperimentOutputCondition(IActiveCampaignTemplateStore activeCampaignTemplateStore, IAnalyzerRepo analyzerManager, IEnumerable<ICampaignValidator> validators)
  {
    _activeCampaignTemplateStore = activeCampaignTemplateStore;
    _analyzerManager = analyzerManager;
    _campaignAnalyzerValidator = validators.OfType<GoodAnalyzerCampaignValidator>().First();
  }

  public async Task<StartConditionResult> CanStart()
  {
    var campaignTemplate = _activeCampaignTemplateStore.CampaignTemplate;
    if(campaignTemplate is null)
      return new StartConditionResult(false, "No campaign template set, can't check for analyzers.");
    if(campaignTemplate.ExperimentTemplate is null)
      return new StartConditionResult(false, "No experiment templates in the campaign, can't check for analyzers.");

    var validation = await _campaignAnalyzerValidator.Validate(campaignTemplate);

    return new StartConditionResult(validation.Success, validation.Messages);
  }
}
