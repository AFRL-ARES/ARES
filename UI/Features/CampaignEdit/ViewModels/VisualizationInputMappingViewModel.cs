using Ares.Datamodel;
using UI.Features.CampaignEdit.Internal;

namespace UI.Features.CampaignEdit.ViewModels;

public sealed class VisualizationInputMappingViewModel(
  string inputKey,
  AresDataType inputType,
  bool required,
  IReadOnlyCollection<VisualizationValueSource> matchingValues,
  string? selectedSourcePath)
{
  public string InputKey { get; } = inputKey;
  public AresDataType InputType { get; } = inputType;
  public bool Required { get; } = required;
  public IReadOnlyCollection<VisualizationValueSource> MatchingValues { get; } = matchingValues;
  public string? SelectedSourcePath { get; set; } = selectedSourcePath;
}
