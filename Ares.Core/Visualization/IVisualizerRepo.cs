namespace Ares.Core.Visualization;

public interface IVisualizerRepo
{
  IEnumerable<IRemoteVisualizer> AvailableVisualizers { get; }
  IRemoteVisualizer? GetVisualizerById(string id);
  internal void AddVisualizer(IRemoteVisualizer visualizer);
  internal void RemoveVisualizer(IRemoteVisualizer visualizer);
  internal void RemoveVisualizer(string visualizerId);
}