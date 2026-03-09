using Ares.Datamodel.Device;
using DynamicData;
using System.Collections;

namespace Ares.Core.Device.Repos;

/// <summary>
/// Implementation of the device config repository. 
/// Responsible for tracking configs that are currently active in the ARES system.
/// </summary>
public class DeviceConfigRepo : IDeviceConfigRepo
{
  private readonly SourceCache<DeviceConfig, string> _configCache = new(c => c.UniqueId);
  public ISourceCache<DeviceConfig, string> Cache => _configCache;

  public DeviceConfig? GetConfig(string id)
  {
    var lookup = _configCache.Lookup(id);
    return lookup.HasValue ? lookup.Value : null;
  }

  public DeviceConfig? GetConfigByDeviceId(string deviceId) 
    =>_configCache.Items.FirstOrDefault(x => x.DeviceId == deviceId);

  public IEnumerator<DeviceConfig> GetEnumerator() => _configCache.Items.GetEnumerator();
  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
  public IReadOnlyCollection<DeviceConfig> GetAll() => _configCache.Items.ToList().AsReadOnly();
  public void AddOrUpdate(DeviceConfig device) => _configCache.AddOrUpdate(device);
  public void Remove(string id) => _configCache.Remove(id);
  public void Clear() => _configCache.Clear();

  public void Dispose()
  {
    _configCache.Dispose();
    GC.SuppressFinalize(this);
  }

}
