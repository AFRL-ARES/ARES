using Ares.Datamodel.Visualizing.Remote;
using DynamicData;

namespace Ares.Core.Visualization.Repos;

public interface IRemoteVisualizationConfigRepo : IDisposable, IEnumerable<RemoteVisualizationConfig>
{
  ISourceCache<RemoteVisualizationConfig, string> Cache { get; }
  RemoteVisualizationConfig? GetConfig(string id);
  IReadOnlyCollection<RemoteVisualizationConfig> GetAll();
  void AddOrUpdate(RemoteVisualizationConfig config);
  void Remove(string id);
}