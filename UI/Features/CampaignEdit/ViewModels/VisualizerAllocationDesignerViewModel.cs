using System.Collections.ObjectModel;
using Ares.Core.Analyzing;
using Ares.Core.Visualization.Providers;
using Ares.Datamodel;
using Ares.Datamodel.Templates;
using Ares.Datamodel.Visualizing;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using UI.Features.CampaignEdit.Internal;

namespace UI.Features.CampaignEdit.ViewModels;

public partial class VisualizerAllocationDesignerViewModel : ReactiveObject
{
  private readonly ExperimentTemplate _experimentTemplate;
  private readonly IEnumerable<CommandDesignerViewModel> _commandDesigners;
  private readonly IEnumerable<CommandDesignerViewModel> _startupCommandDesigners;
  private readonly Func<string?> _analyzerIdProvider;
  private readonly IAnalyzerRepo _analyzerRepo;
  private readonly IVisualizerProvider _visualizerProvider;

  public VisualizerAllocationDesignerViewModel(
    ExperimentTemplate experimentTemplate,
    IEnumerable<CommandDesignerViewModel> commandDesigners,
    IEnumerable<CommandDesignerViewModel> startupCommandDesigners,
    Func<string?> analyzerIdProvider,
    IAnalyzerRepo analyzerRepo,
    IVisualizerProvider visualizerProvider)
  {
    _experimentTemplate = experimentTemplate;
    _commandDesigners = commandDesigners;
    _startupCommandDesigners = startupCommandDesigners;
    _analyzerIdProvider = analyzerIdProvider;
    _analyzerRepo = analyzerRepo;
    _visualizerProvider = visualizerProvider;

    AllocationEditors = new ObservableCollection<VisualizerAllocationEditorViewModel>(experimentTemplate.VisualizerAllocations.Select(CreateEditor));
  }

  [Reactive]
  public partial bool IsLoading { get; private set; }

  [Reactive]
  public partial string? LoadError { get; private set; }

  public ObservableCollection<VisualizerAllocationEditorViewModel> AllocationEditors { get; }

  public bool HasAllocations => AllocationEditors.Count > 0;

  public async Task InitializeAsync()
  {
    IsLoading = true;
    LoadError = null;

    try
    {
      var availableValues = await GetAvailableValues();
      foreach(var editor in AllocationEditors)
      {
        editor.UpdateAvailableValues(availableValues);
        await editor.InitializeAsync();
      }
    }
    catch(Exception exception)
    {
      LoadError = exception.Message;
    }
    finally
    {
      IsLoading = false;
    }
  }

  public async Task AddAllocation()
  {
    var editor = CreateEditor(new VisualizerAllocation
    {
      UniqueId = Guid.NewGuid().ToString()
    });
    editor.UpdateAvailableValues(await GetAvailableValues());
    await editor.InitializeAsync();
    AllocationEditors.Add(editor);
    this.RaisePropertyChanged(nameof(HasAllocations));
  }

  public void RemoveAllocation(VisualizerAllocationEditorViewModel editor)
  {
    AllocationEditors.Remove(editor);
    this.RaisePropertyChanged(nameof(HasAllocations));
  }

  public void Save()
  {
    var allocations = AllocationEditors.Select(editor => editor.Save()).ToArray();
    var duplicateIds = allocations.GroupBy(allocation => allocation.UniqueId).FirstOrDefault(group => group.Count() > 1);

    if(duplicateIds is not null)
      throw new InvalidOperationException("Visualization allocations must have unique internal identifiers.");

    _experimentTemplate.VisualizerAllocations.Clear();
    _experimentTemplate.VisualizerAllocations.AddRange(allocations);
  }

  private VisualizerAllocationEditorViewModel CreateEditor(VisualizerAllocation allocation)
    => new(allocation, _visualizerProvider);

  private async Task<IReadOnlyCollection<VisualizationValueSource>> GetAvailableValues()
  {
    await Task.WhenAll(_commandDesigners
      .Concat(_startupCommandDesigners)
      .Select(command => command.EnsureInitializedAsync()));

    var values = new List<VisualizationValueSource>();
    values.AddRange(_commandDesigners.SelectMany(command => GetOutputSchemaPaths(command, "Experiment output")));
    values.AddRange(_startupCommandDesigners.SelectMany(command => GetOutputSchemaPaths(command, "Startup output")));

    var analyzerId = _analyzerIdProvider();
    if(!string.IsNullOrWhiteSpace(analyzerId))
    {
      var analyzer = _analyzerRepo.GetAnalyzerById(analyzerId);
      if(analyzer is not null)
      {
        var objectiveSchema = await analyzer.GetObjectiveOutputs();
        values.AddRange(GetSchemaPaths(objectiveSchema, "Analyzer objective"));
      }
    }

    return values
      .GroupBy(value => value.Path)
      .Select(group => group.First())
      .ToArray();
  }

  private static IEnumerable<VisualizationValueSource> GetOutputSchemaPaths(CommandDesignerViewModel commandDesigner, string sourceName)
  {
    if(!commandDesigner.OutputProvider || string.IsNullOrWhiteSpace(commandDesigner.OutputVariableName))
      return [];

    var outputSchema = commandDesigner.OutputSchema;
    if(outputSchema is null)
      return [];

    return GetSchemaPaths(commandDesigner.OutputVariableName, outputSchema, sourceName);
  }

  private static IEnumerable<VisualizationValueSource> GetSchemaPaths(AresStructSchema schema, string sourceName)
    => schema.Fields.SelectMany(field => GetSchemaPaths(field.Key, field.Value, sourceName));

  private static IEnumerable<VisualizationValueSource> GetSchemaPaths(string path, AresValueSchema schema, string sourceName)
  {
    yield return new VisualizationValueSource(path, schema.Type, $"{sourceName}: {path}");

    if(schema.Type != AresDataType.Struct || schema.StructSchema is null)
      yield break;

    foreach(var field in schema.StructSchema.Fields)
    {
      foreach(var nestedPath in GetSchemaPaths($"{path}.{field.Key}", field.Value, sourceName))
        yield return nestedPath;
    }
  }
}