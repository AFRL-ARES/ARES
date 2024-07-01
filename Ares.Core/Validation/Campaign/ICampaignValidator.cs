using Ares.Messaging;

namespace Ares.Core.Validation.Campaign;

public interface ICampaignValidator
{
  /// <summary>
  /// Checks a given campaign template to make sure it's valid
  /// </summary>
  /// <param name="template"></param>
  /// <returns></returns>
  ValidationResult Validate(CampaignTemplate template);
}
