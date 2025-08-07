using Ares.Datamodel.Templates;
using UI.Backend.ViewModels.Automation.CampaignEdit;

namespace UI.Backend.ViewModels.Factories;

public class PlannableParameterDesignerFactory
{
  private readonly ParameterEditorFactory _parameterEditorFactory;

  public PlannableParameterDesignerFactory(ParameterEditorFactory parameterEditorFactory)
  {
    _parameterEditorFactory = parameterEditorFactory;
  }

  public PlannableParameterDesignerViewModel Create(IEnumerable<ParameterMetadata> existingParameterMeta)
    => new(existingParameterMeta, _parameterEditorFactory);
}
