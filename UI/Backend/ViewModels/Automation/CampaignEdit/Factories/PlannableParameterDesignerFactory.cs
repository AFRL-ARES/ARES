using Ares.Datamodel.Templates;

namespace UI.Backend.ViewModels.Automation.CampaignEdit.Factories;

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
