using Ares.Core.Analyzing;
using Ares.Core.Validation.Validators;
using Ares.Messaging;

namespace Ares.Core.Validation.Campaign;

public class GoodAnalyzerCampaignValidator : ICampaignValidator
{
  private readonly IAnalyzerManager _analyzerManager;

  public GoodAnalyzerCampaignValidator(IAnalyzerManager analyzerManager)
  {
    _analyzerManager = analyzerManager;
  }

  public ValidationResult Validate(CampaignTemplate template)
    => GoodAnalyzerValidator.Validate(template.ExperimentTemplates, _analyzerManager);
}
