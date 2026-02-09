using Ares.Datamodel.Templates;
using UI.Features.CampaignEdit.ViewModels;

namespace UI.Features.CampaignEdit.Factories;

public class PlannableParameterDesignerFactory
{
  private readonly ParameterEditorFactory _parameterEditorFactory;

  public PlannableParameterDesignerFactory(ParameterEditorFactory parameterEditorFactory)
  {
    _parameterEditorFactory = parameterEditorFactory;
  }

  public PlannableParameterDesignerViewModel Create(IEnumerable<ParameterMetadata> existingParameterMeta, ExperimentTemplate? experimentTemplate)
    => new(existingParameterMeta, experimentTemplate, _parameterEditorFactory);
}
