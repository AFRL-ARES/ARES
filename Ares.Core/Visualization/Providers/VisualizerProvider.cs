using Ares.Core.Visualization.Repos;
using Ares.Datamodel.Visualizing.Remote;

namespace Ares.Core.Visualization.Providers;

public sealed class VisualizerProvider : IVisualizerProvider
{
  private readonly IVisualizerRepo _visualizerRepo;

  public VisualizerProvider(IVisualizerRepo visualizerRepo)
  {
    _visualizerRepo = visualizerRepo;
  }

  public IReadOnlyCollection<IRemoteVisualizer> GetAllVisualizers()
    => _visualizerRepo.AvailableVisualizers.ToArray();

  public IRemoteVisualizer? GetVisualizer(string visualizerId)
    => _visualizerRepo.GetVisualizerById(visualizerId);

  public Task<VisualizationOptionsResponse> GetVisualizationOptions(string visualizerId, CancellationToken cancellationToken = default)
  {
    var visualizer = GetVisualizer(visualizerId) ?? throw new InvalidOperationException($"Visualizer '{visualizerId}' was not found.");
    return visualizer.GetVisualizationOptions(cancellationToken);
  }
}