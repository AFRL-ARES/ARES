using Ares.Datamodel.Templates;
using UI.Components.Formatting;
using ParameterEditorViewModel = UI.Features.CampaignEdit.ViewModels.ParameterEditorViewModel;

namespace UI.Features.CampaignEdit.Factories;

public class ParameterEditorFactory
{
  private readonly UnitCategoryHelper _unitCategoryHelper;

  public ParameterEditorFactory(UnitCategoryHelper unitCategoryHelper)
  {
    _unitCategoryHelper = unitCategoryHelper;
  }

  public ParameterEditorViewModel Create(IEnumerable<string> availableOutputs) => new(_unitCategoryHelper, availableOutputs);

  public ParameterEditorViewModel Create(ParameterMetadata existingParameterMetadata,  IEnumerable<string> availableOutputs) => new(existingParameterMetadata, availableOutputs, _unitCategoryHelper);
}
