using Ares.Datamodel.Templates;
using UI.Backend.Helpers;
using UI.Backend.ViewModels.Automation.CampaignEdit;
using UI.Services.CampaignEdit;

namespace UI.Backend.ViewModels.Factories;

public class CommandParameterDesignerFactory
{
  private readonly CampaignEditContext _campaignEditContext;
  private readonly UnitCategoryHelper _unitCategoryHelper;

  public CommandParameterDesignerFactory(CampaignEditContext campaignEditContext, UnitCategoryHelper unitCategoryHelper)
  {
    _campaignEditContext = campaignEditContext;
    _unitCategoryHelper = unitCategoryHelper;
  }

  public CommandParameterDesignerViewModel Create(ParameterMetadata existingParameterMetadata) => new(existingParameterMetadata, _unitCategoryHelper, _campaignEditContext.CurrentlyEditingCampaign?.PlannableParameters);

  public CommandParameterDesignerViewModel Create(Parameter existingParameter) => new(existingParameter, _unitCategoryHelper, _campaignEditContext.CurrentlyEditingCampaign?.PlannableParameters);
}
