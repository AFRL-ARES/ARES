using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Ares.Core.Device;

public class DeviceDriverRepo : IDeviceDriverRepo
{
  private readonly ConcurrentDictionary<string, DeviceDriver> _drivers = new(StringComparer.OrdinalIgnoreCase);

  public void Register(DeviceDriver driver)
  {
    if(driver == null) throw new ArgumentNullException(nameof(driver));
    _drivers[driver.Manifest.Name] = driver;
  }

  public DeviceDriver? GetByName(string name)
  {
    return _drivers.TryGetValue(name, out var driver) ? driver : null;
  }

  public IEnumerable<DeviceDriver> GetAll()
  {
    return _drivers.Values;
  }
}
