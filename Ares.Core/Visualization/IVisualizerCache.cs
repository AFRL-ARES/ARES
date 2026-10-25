using Ares.Datamodel;
using Ares.Datamodel.Analyzing;
using Ares.Datamodel.Visualizing;

namespace Ares.Core.Visualization;

public interface IVisualizerCache
{
  Task CacheVisualizerInfo(RemoteVisualizer visualizer);
  Task CacheVisualizerSettings(RemoteVisualizer visualizer);
  Task<VisualizerInfo?> GetCachedVisualizerInfo(string visualizerId);
  Task<AresStruct?> GetCachedVisualizerSettings(string visualizerId);
}
