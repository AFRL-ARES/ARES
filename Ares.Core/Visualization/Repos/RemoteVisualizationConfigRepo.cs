using Ares.Datamodel.Visualizing.Remote;
using DynamicData;
using System.Collections;

namespace Ares.Core.Visualization.Repos;

public sealed class RemoteVisualizationConfigRepo : IRemoteVisualizationConfigRepo
{
  private readonly SourceCache<RemoteVisualizationConfig, string> _configCache = new(config => config.UniqueId);

  public RemoteVisualizationConfig? GetConfig(string id)
  {
    var lookup = _configCache.Lookup(id);
    return lookup.HasValue ? lookup.Value : null;
  }

  public IReadOnlyCollection<RemoteVisualizationConfig> GetAll()
    => _configCache.Items.ToList().AsReadOnly();

  public void AddOrUpdate(RemoteVisualizationConfig config)
    => _configCache.AddOrUpdate(config);

  public void Remove(string id)
    => _configCache.Remove(id);

  public IEnumerator<RemoteVisualizationConfig> GetEnumerator()
    => _configCache.Items.GetEnumerator();

  IEnumerator IEnumerable.GetEnumerator()
    => GetEnumerator();

  public void Dispose()
  {
    _configCache.Dispose();
    GC.SuppressFinalize(this);
  }

  public ISourceCache<RemoteVisualizationConfig, string> Cache => _configCache;
}