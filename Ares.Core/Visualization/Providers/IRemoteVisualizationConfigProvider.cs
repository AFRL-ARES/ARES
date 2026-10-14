using Ares.Datamodel.Visualizing.Remote;
using DynamicData;

namespace Ares.Core.Visualization.Providers;

public interface IRemoteVisualizationConfigProvider : IEnumerable<RemoteVisualizationConfig>
{
  IObservable<IChangeSet<RemoteVisualizationConfig, string>> Connect();
  RemoteVisualizationConfig? GetConfig(string id);
  IReadOnlyCollection<RemoteVisualizationConfig> GetAllConfigs();
}