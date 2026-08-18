using System.Collections.ObjectModel;
using Ares.Datamodel.Templates;
using DynamicData;
using ReactiveUI;
using UI.Features.CampaignEdit.Factories;
using UI.Domain.Experiments;

namespace UI.Features.CampaignEdit.ViewModels;

public class PlannableParameterDesignerViewModel : ReactiveObject
{
  private readonly ParameterEditorFactory _editorFactory;
  private IEnumerable<ParameterMetadata> _parameterMetadata = [];
  private ExperimentTemplate? _experimentTemplate;

  public PlannableParameterDesignerViewModel(IEnumerable<ParameterMetadata> existingMetadata, ExperimentTemplate? experimentTemplate, ParameterEditorFactory editorFactory)
  {
    _editorFactory = editorFactory;
    _experimentTemplate = experimentTemplate;
    ParameterMetadata = existingMetadata;
  }

  public ObservableCollection<ParameterEditorViewModel> ParameterEditors { get; } = [];

  public IEnumerable<ParameterMetadata> ParameterMetadata
  {
    private get => _parameterMetadata;

    set
    {
      _parameterMetadata = value;
      Init(value);
    }
  }

  public IEnumerable<ParameterMetadata> Save()
  {
    return ParameterEditors.Select(model => model.Save());
  }

  private void Init(IEnumerable<ParameterMetadata> paramMetadata)
  {
    var outputs = GetExperimentOutputNames();
    ParameterEditors.AddRange(paramMetadata.Select(metadata => _editorFactory.Create(metadata, outputs)));
  }

  public void Create()
  {
    var outputs = GetExperimentOutputNames();
    ParameterEditors.Add(_editorFactory.Create(outputs));
  }

  private string[] GetExperimentOutputNames()
    => _experimentTemplate?.GetAllOutputCommands()
      .Where(cmd => !string.IsNullOrWhiteSpace(cmd.OutputVarName))
      .Select(cmd => cmd.OutputVarName)
      .ToArray() ?? [];

  public void Remove(ParameterEditorViewModel vm)
  {
    ParameterEditors.Remove(vm);
  }
}
