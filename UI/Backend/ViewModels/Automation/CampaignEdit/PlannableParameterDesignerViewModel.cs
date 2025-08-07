using System.Collections.ObjectModel;
using Ares.Datamodel.Templates;
using DynamicData;
using ReactiveUI;
using UI.Backend.ViewModels.Factories;

namespace UI.Backend.ViewModels.Automation.CampaignEdit;

public class PlannableParameterDesignerViewModel : ReactiveObject
{
  private readonly ParameterEditorFactory _editorFactory;
  private IEnumerable<ParameterMetadata> _parameterMetadata = Array.Empty<ParameterMetadata>();

  public PlannableParameterDesignerViewModel(IEnumerable<ParameterMetadata> existingMetadata, ParameterEditorFactory editorFactory)
  {
    _editorFactory = editorFactory;
    ParameterMetadata = existingMetadata;
  }

  public ObservableCollection<ParameterEditorViewModel> ParameterEditors { get; } = new();

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
    ParameterEditors.AddRange(paramMetadata.Select(metadata => _editorFactory.Create(metadata)));
  }

  public void Create()
  {
    ParameterEditors.Add(_editorFactory.Create());
  }

  public void Remove(ParameterEditorViewModel vm)
  {
    ParameterEditors.Remove(vm);
  }
}
