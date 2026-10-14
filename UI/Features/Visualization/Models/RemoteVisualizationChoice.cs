namespace UI.Features.Visualization.Models;

public sealed record RemoteVisualizationChoice(
  string VisualizerId,
  string VisualizerName,
  string VisualName,
  string DisplayName);