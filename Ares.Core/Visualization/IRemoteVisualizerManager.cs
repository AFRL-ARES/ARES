using Ares.Datamodel.Analyzing;
using Ares.Datamodel.Visualizing;

namespace Ares.Core.Visualization;

public interface IRemoteVisualizerManager
{
  Task LoadVisualizers();
  Task CreateVisualizer(string name, string url);
  Task RemoveVisualizer(string visualizerId);
  Task UpdateVisualizer(VisualizerConfig config);
  Task UpdateVisualizerSettings(VisualizerSettings visualizerSettings);
}