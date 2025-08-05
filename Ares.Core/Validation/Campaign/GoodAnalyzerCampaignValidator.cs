using Ares.Core.Analyzing;
using Ares.Core.Validation.Validators;
using Ares.Messaging;

namespace Ares.Core.Validation.Campaign;

public class GoodAnalyzerCampaignValidator : ICampaignValidator
{
  private readonly IAnalyzerRepo _analyzerManager;

  public GoodAnalyzerCampaignValidator(IAnalyzerRepo analyzerManager)
  {
    _analyzerManager = analyzerManager;
  }

  public Task<ValidationResult> Validate(CampaignTemplate template)
    => GoodAnalyzerValidator.Validate(template.ExperimentTemplates, _analyzerManager);
}
