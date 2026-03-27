using Ares.Device;

namespace UI.Features.Visualization;

public class ChartCreationRequest
{
  public required IAresDevice Device { get; init; }
  public required VisualizationPath Path { get; init; }
  public ChartStyle SelectedStyle { get; init; }
  public string Title => $"{Device.Name} - {Path.Path}";
}
