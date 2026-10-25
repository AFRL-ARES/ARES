using Ares.Core.Visualization;
using Ares.Core.Visualization.Providers;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Visualizing;
using Ares.Datamodel.Visualizing.Remote;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System.Collections.ObjectModel;
using UI.Features.CampaignEdit.Internal;

namespace UI.Features.CampaignEdit.ViewModels;

public sealed partial class VisualizerAllocationEditorViewModel : ReactiveObject
{
  private readonly IVisualizerProvider _visualizerProvider;
  private readonly Dictionary<string, string> _persistedMappings;
  private IReadOnlyCollection<VisualizationValueSource> _availableValues = [];
  private string? _selectedVisualizerId;
  private string? _selectedVisualizationName;
  private string _userProvidedIdentifier;

  public VisualizerAllocationEditorViewModel(
    VisualizerAllocation allocation,
    IVisualizerProvider visualizerProvider)
  {
    _visualizerProvider = visualizerProvider;
    UniqueId = string.IsNullOrWhiteSpace(allocation.UniqueId)
      ? Guid.NewGuid().ToString()
      : allocation.UniqueId;
    _selectedVisualizerId = allocation.Visualizer?.UniqueId;
    _selectedVisualizationName = allocation.RequestedVisual;
    _userProvidedIdentifier = allocation.UserProvidedIdentifier;
    _persistedMappings = allocation.VisualizationMaps.ToDictionary();
  }

  [Reactive]
  public partial IReadOnlyCollection<IRemoteVisualizer> AvailableVisualizers { get; private set; } = [];

  [Reactive]
  public partial IReadOnlyCollection<VisualizationOption> AvailableVisualizations { get; private set; } = [];

  [Reactive]
  public partial string? SelectionError { get; private set; }

  public string UniqueId { get; }

  public ObservableCollection<VisualizationInputMappingViewModel> InputMappings { get; } = [];

  public string? SelectedVisualizerId
  {
    get => _selectedVisualizerId;
    set
    {
      if(_selectedVisualizerId == value)
        return;

      this.RaiseAndSetIfChanged(ref _selectedVisualizerId, value);
      _ = SelectVisualizerAsync();
    }
  }

  public string? SelectedVisualizationName
  {
    get => _selectedVisualizationName;
    set
    {
      if(_selectedVisualizationName == value)
        return;

      this.RaiseAndSetIfChanged(ref _selectedVisualizationName, value);
      RebuildInputMappings();
    }
  }

  public string UserProvidedIdentifier
  {
    get => _userProvidedIdentifier;
    set => this.RaiseAndSetIfChanged(ref _userProvidedIdentifier, value);
  }

  public VisualizationOption? SelectedVisualization
    => AvailableVisualizations.FirstOrDefault(option => option.VisualizationName == SelectedVisualizationName);

  public async Task InitializeAsync()
  {
    AvailableVisualizers = _visualizerProvider.GetAllVisualizers();
    if(!string.IsNullOrWhiteSpace(SelectedVisualizerId))
      await LoadVisualizationOptions();
  }

  public void UpdateAvailableValues(IReadOnlyCollection<VisualizationValueSource> availableValues)
  {
    _availableValues = availableValues;
    RebuildInputMappings();
  }

  public VisualizerAllocation Save()
  {
    if(string.IsNullOrWhiteSpace(UniqueId))
      throw new InvalidOperationException("Visualization allocation is missing its internal identifier.");

    if(string.IsNullOrWhiteSpace(UserProvidedIdentifier))
      throw new InvalidOperationException("Each visualization hook requires a chart identifier.");

    if(string.IsNullOrWhiteSpace(SelectedVisualizerId))
      throw new InvalidOperationException($"Visualization hook '{UserProvidedIdentifier}' does not select a visualizer.");

    if(string.IsNullOrWhiteSpace(SelectedVisualizationName))
      throw new InvalidOperationException($"Visualization hook '{UserProvidedIdentifier}' does not select a visualization.");

    var visualizer = _visualizerProvider.GetVisualizer(SelectedVisualizerId)
      ?? throw new InvalidOperationException($"Visualizer '{SelectedVisualizerId}' was not found.");
    var mappings = GetMappings();

    foreach(var mapping in InputMappings.Where(mapping => mapping.Required && string.IsNullOrWhiteSpace(mapping.SelectedSourcePath)))
    {
      throw new InvalidOperationException(
        $"Visualization hook '{UserProvidedIdentifier}' is missing a mapping for required input '{mapping.InputKey}'.");
    }

    var allocation = new VisualizerAllocation
    {
      UniqueId = UniqueId,
      Visualizer = CreateVisualizerInfo(visualizer),
      RequestedVisual = SelectedVisualizationName,
      UserProvidedIdentifier = UserProvidedIdentifier.Trim()
    };
    allocation.VisualizationMaps.Add(mappings);
    return allocation;
  }

  private async Task SelectVisualizerAsync()
  {
    AvailableVisualizations = [];
    InputMappings.Clear();
    _persistedMappings.Clear();
    SelectedVisualizationName = null;
    SelectionError = null;

    if(!string.IsNullOrWhiteSpace(SelectedVisualizerId))
      await LoadVisualizationOptions();
  }

  private async Task LoadVisualizationOptions()
  {
    if(string.IsNullOrWhiteSpace(SelectedVisualizerId))
      return;

    try
    {
      var options = await _visualizerProvider.GetVisualizationOptions(SelectedVisualizerId);
      AvailableVisualizations = options.VisualizationOptions.ToArray();
      RebuildInputMappings();
    }
    catch(Exception exception)
    {
      SelectionError = exception.Message;
      AvailableVisualizations = [];
      InputMappings.Clear();
    }
  }

  private void RebuildInputMappings()
  {
    var visualization = SelectedVisualization;
    if(visualization is null)
      return;

    var existingMappings = GetMappings();
    InputMappings.Clear();
    foreach(var field in visualization.VisualizationInputSchema.Fields)
    {
      var compatibleValues = _availableValues
        .Where(value => AresValueHelper.AreCompatibleDataTypes(field.Value.Type, value.Type))
        .ToList();
      existingMappings.TryGetValue(field.Key, out var selectedSourcePath);

      if(!string.IsNullOrWhiteSpace(selectedSourcePath) && compatibleValues.All(value => value.Path != selectedSourcePath))
      {
        compatibleValues.Add(new VisualizationValueSource(
          selectedSourcePath,
          field.Value.Type,
          $"Unavailable: {selectedSourcePath}"));
      }

      InputMappings.Add(new VisualizationInputMappingViewModel(
        field.Key,
        field.Value.Type,
        !field.Value.Optional,
        compatibleValues,
        selectedSourcePath));
    }
  }

  private Dictionary<string, string> GetMappings()
  {
    if(InputMappings.Count == 0)
      return new Dictionary<string, string>(_persistedMappings);

    return InputMappings
      .Where(mapping => !string.IsNullOrWhiteSpace(mapping.SelectedSourcePath))
      .ToDictionary(mapping => mapping.InputKey, mapping => mapping.SelectedSourcePath!);
  }

  private static VisualizerInfo CreateVisualizerInfo(IRemoteVisualizer visualizer)
    => new()
    {
      UniqueId = visualizer.UniqueId,
      Name = visualizer.Name,
      Type = visualizer.Type,
      Version = visualizer.Version,
      Description = visualizer.Description,
      Url = visualizer.Address.ToString()
    };
}