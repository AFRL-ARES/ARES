using Ares.Core.Validation.Campaign;

namespace Ares.Core.Execution.StartConditions;

internal class AllPlannersAssignedStartCondition : IStartCondition
{
  private readonly IActiveCampaignTemplateStore _activeCampaignTemplateStore;
  private readonly ICampaignValidator _allPlannersValidator;

  public AllPlannersAssignedStartCondition(IActiveCampaignTemplateStore activeCampaignTemplateStore, IEnumerable<ICampaignValidator> campaignValidators)
  {
    _activeCampaignTemplateStore = activeCampaignTemplateStore;
    _allPlannersValidator = campaignValidators.OfType<AllPlannersAssignedCampaignValidator>().First();
  }

  public StartConditionResult? CanStart()
  {
    if (_activeCampaignTemplateStore.CampaignTemplate is null)
      return null;

    var validation = _allPlannersValidator.Validate(_activeCampaignTemplateStore.CampaignTemplate);
    return new StartConditionResult(validation.Success, validation.Messages);
  }
}
