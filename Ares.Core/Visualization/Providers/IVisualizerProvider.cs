using Ares.Datamodel.Visualizing.Remote;

namespace Ares.Core.Visualization.Providers;

/// <summary>
/// Mediates read-only access to the locally registered visualization services.
/// </summary>
public interface IVisualizerProvider
{
  IReadOnlyCollection<IRemoteVisualizer> GetAllVisualizers();
  IRemoteVisualizer? GetVisualizer(string visualizerId);
  Task<VisualizationOptionsResponse> GetVisualizationOptions(string visualizerId, CancellationToken cancellationToken = default);
}