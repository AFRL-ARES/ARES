using Ares.Device;
using DynamicData;
using System.Collections;

namespace Ares.Core.Device.Repos;

/// <summary>
/// Implementation of the device repository. 
/// Responsible for tracking device that are currently active in the ARES system.
/// </summary>
public class AresDeviceRepo : IAresDeviceRepo
{
  private readonly SourceCache<IAresDevice, string> _deviceCache = new(d => d.UniqueId);

  public ISourceCache<IAresDevice, string> Cache => _deviceCache;

  public IAresDevice? GetDevice(string id)
  {
    var lookup = _deviceCache.Lookup(id);
    return lookup.HasValue ? lookup.Value : null;
  }

  public T? GetDevice<T>(string id) where T : class, IAresDevice
  {
    return GetDevice(id) as T;
  }

  public IReadOnlyCollection<T> GetAll<T>() where T : IAresDevice
  {
    return _deviceCache.Items.OfType<T>().ToList().AsReadOnly();
  }

  public IEnumerator<IAresDevice> GetEnumerator() => _deviceCache.Items.GetEnumerator();
  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
  public IReadOnlyCollection<IAresDevice> GetAll() => _deviceCache.Items.ToList().AsReadOnly();
  public void AddOrUpdate(IAresDevice device) => _deviceCache.AddOrUpdate(device);
  public void Remove(string id) => _deviceCache.Remove(id);
  public void Clear() => _deviceCache.Clear();

  public void Dispose()
  {
    _deviceCache.Dispose();
    GC.SuppressFinalize(this);
  }
}
