using System.Collections.ObjectModel;

namespace Ares.Core.Visualization;

public class VisualizerRepo : IVisualizerRepo
{
  private readonly IList<IRemoteVisualizer> _visualizerStore = [];

  public IEnumerable<IRemoteVisualizer> AvailableVisualizers
    => new ReadOnlyCollection<IRemoteVisualizer>(_visualizerStore);

  public IRemoteVisualizer? GetVisualizerById(string id)
    => _visualizerStore.FirstOrDefault(visualizer => visualizer.UniqueId == id);

  public void AddVisualizer(IRemoteVisualizer visualizer)
  {
    var visualizerExists = _visualizerStore.Any(existing =>
      existing == visualizer ||
      (existing.Name == visualizer.Name && existing.Version == visualizer.Version && existing.Type == visualizer.Type));

    if(visualizerExists)
      throw new InvalidOperationException($"Visualizer {visualizer.Name}{visualizer.Version} of type {visualizer.Type} already registered");

    _visualizerStore.Add(visualizer);
  }

  public void RemoveVisualizer(IRemoteVisualizer visualizer)
    => _visualizerStore.Remove(visualizer);
  

  public void RemoveVisualizer(string visualizerId)
  {
    var visualizer = GetVisualizerById(visualizerId);
    if(visualizer is not null)
      RemoveVisualizer(visualizer);
  }
}