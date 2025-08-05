using Ares.Core.Validation.Campaign;

namespace Ares.Core.Execution.StartConditions;

internal class RequiredDeviceInterpretersStartCondition : IStartCondition
{
  private readonly ICampaignValidator _requiredDeviceCommandInterpretersValidator;
  private readonly IActiveCampaignTemplateStore _activeCampaignTemplateStore;
  public RequiredDeviceInterpretersStartCondition(IActiveCampaignTemplateStore activeCampaignTemplateStore,
    IEnumerable<ICampaignValidator> validators)
  {
    _activeCampaignTemplateStore = activeCampaignTemplateStore;
    _requiredDeviceCommandInterpretersValidator = validators.OfType<RequiredDeviceInterpretersValidator>().First();
  }
  public async Task<StartConditionResult> CanStart()
  {
    var template = _activeCampaignTemplateStore.CampaignTemplate;
    if(template is null)
      return new StartConditionResult(false, "Campaign template not set");

    var validationResult =
      await _requiredDeviceCommandInterpretersValidator.Validate(template);
    var startConditionResult = new StartConditionResult(validationResult.Success, validationResult.Messages);
    return startConditionResult;
  }
}
