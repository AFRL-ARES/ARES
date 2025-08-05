using Ares.Core.Execution.Extensions;
using Ares.Core.Validation.Validators;
using Ares.Messaging;

namespace Ares.Core.Validation.Campaign;

public class AllPlannersAssignedCampaignValidator : ICampaignValidator
{
  public Task<ValidationResult> Validate(CampaignTemplate template)
  {
    var parameters = template.ExperimentTemplates.SelectMany(experimentTemplate => experimentTemplate.GetAllPlannedParameters());
    return Task.FromResult(AllPlannersAssignedValidator.Validate(parameters, template.PlannerAllocations));
  }
}
