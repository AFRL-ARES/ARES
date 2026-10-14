namespace Ares.Core.Visualization;

public sealed record LatestRemoteVisualization(
  string SourceKey,
  string VisualizerId,
  string RequestedVisual,
  string ChartJson,
  DateTime GeneratedAt,
  string VisualizerName);