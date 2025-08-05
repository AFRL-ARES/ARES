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

  public async Task<StartConditionResult> CanStart()
  {
    if(_activeCampaignTemplateStore.CampaignTemplate is null)
      return new StartConditionResult(false, "No campaign template selected, cannot check for planners.");

    var validation = await _allPlannersValidator.Validate(_activeCampaignTemplateStore.CampaignTemplate);
    return new StartConditionResult(validation.Success, validation.Messages);
  }
}
