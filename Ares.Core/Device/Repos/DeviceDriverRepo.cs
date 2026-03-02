using DynamicData;
using System.Collections;

namespace Ares.Core.Device.Repos;

public class DeviceDriverRepo : IDeviceDriverRepo
{
  private readonly SourceCache<DeviceDriver, string> _driverCache = new(d => d.UniqueId);

  public ISourceCache<DeviceDriver, string> Cache => _driverCache;

  public DeviceDriver? GetDriverById(string id)
  {
    var lookup = _driverCache.Lookup(id);
    return lookup.HasValue ? lookup.Value : null;
  }

  public DeviceDriver? GetDriverByName(string name)
  {
    var driver = _driverCache.Items.FirstOrDefault(d => d.Manifest.DeviceTypeName == name);
    return driver;
  }

  public IReadOnlyCollection<DeviceDriver> GetAllDrivers() => _driverCache.Items.ToList().AsReadOnly();
  public IEnumerator<DeviceDriver> GetEnumerator() => GetEnumerator();
  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

  public void AddOrUpdate(DeviceDriver driver) => _driverCache.AddOrUpdate(driver);
  public void Remove(string id) => _driverCache.Remove(id);
  public void Clear() => _driverCache.Clear();

  public void Dispose()
  {
    _driverCache.Dispose();
    GC.SuppressFinalize(this);
  }
}