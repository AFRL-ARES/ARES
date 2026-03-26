using Ares.Datamodel;

namespace UI.Features.Visualization;

public class VisualizationPath
{
  public string Path { get; init; } = string.Empty;
  public AresDataType DataType { get; init; }

  // Only numbers and booleans are safe to plot by default
  public bool IsPlottable => DataType == AresDataType.Number || DataType == AresDataType.Boolean || DataType == AresDataType.Quantity;
}