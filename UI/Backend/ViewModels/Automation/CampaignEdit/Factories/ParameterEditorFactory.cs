using Ares.Datamodel.Templates;
using UI.Backend.Helpers;
using UI.Backend.ViewModels.Automation.CampaignEdit;

namespace UI.Backend.ViewModels.Factories;

public class ParameterEditorFactory
{
  private readonly UnitCategoryHelper _unitCategoryHelper;

  public ParameterEditorFactory(UnitCategoryHelper unitCategoryHelper)
  {
    _unitCategoryHelper = unitCategoryHelper;
  }

  public ParameterEditorViewModel Create() => new(_unitCategoryHelper);

  public ParameterEditorViewModel Create(ParameterMetadata existingParameterMetadata) => new(existingParameterMetadata, _unitCategoryHelper);
}
