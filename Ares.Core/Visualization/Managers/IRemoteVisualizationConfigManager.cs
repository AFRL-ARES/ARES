using Ares.Datamodel.Visualizing.Remote;

namespace Ares.Core.Visualization.Managers;

public interface IRemoteVisualizationConfigManager
{
  Task Initialize();
  Task Add(RemoteVisualizationConfig config);
  Task Remove(string configId);
  Task Update(RemoteVisualizationConfig config);
}