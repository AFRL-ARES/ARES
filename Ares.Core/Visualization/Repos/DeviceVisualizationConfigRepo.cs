using Ares.Core.Visualization.Helpers;
using Ares.Datamodel.Visualizing.Local;
using DynamicData;
using System.Collections;

namespace Ares.Core.Visualization.Repos;

public class DeviceVisualizationConfigRepo : IDeviceVisualizationConfigRepo
{
  private readonly SourceCache<DeviceVisualizationConfig, string> _configCache = new(c => c.UniqueId);
  public ISourceCache<DeviceVisualizationConfig, string> Cache => _configCache;

  public DeviceVisualizationConfig? GetConfig(string id)
  {
    var lookup = _configCache.Lookup(id);
    return lookup.HasValue ? lookup.Value : null;
  }

  public IEnumerable<DeviceVisualizationConfig> GetConfigsByDeviceId(string deviceId) 
    => _configCache.Items.Where(c => c.GetAssociatedDeviceIds().Any(id => id == deviceId));

  public IEnumerator<DeviceVisualizationConfig> GetEnumerator() => _configCache.Items.GetEnumerator();
  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
  public IReadOnlyCollection<DeviceVisualizationConfig> GetAll() => _configCache.Items.ToList().AsReadOnly();
  public void AddOrUpdate(DeviceVisualizationConfig config) => _configCache.AddOrUpdate(config);
  public void Remove(string id) => _configCache.Remove(id);
  public void Clear() => _configCache.Clear();
  
  public void Dispose()
  {
    _configCache.Dispose();
    GC.SuppressFinalize(this);
  }
}
