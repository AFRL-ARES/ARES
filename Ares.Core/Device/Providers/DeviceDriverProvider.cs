using Ares.Core.Device.Plugins.Drivers;
using Ares.Core.Device.Repos;
using DynamicData;
using System.Collections;

namespace Ares.Core.Device.Providers;

/// <summary>
/// Mediates Read-Only access to the ARES device driver repository.
/// </summary>
public class DeviceDriverProvider : IDeviceDriverProvider
{
  private readonly IDeviceDriverRepo _driverRepo;

  public DeviceDriverProvider(IDeviceDriverRepo driverRepo)
  {
    _driverRepo = driverRepo;    
  }

  public IReadOnlyCollection<DeviceDriver> GetAllDeviceDrivers() => _driverRepo.GetAllDrivers();
  public IObservable<IChangeSet<DeviceDriver, string>> Connect() => _driverRepo.Cache.Connect();
  public DeviceDriver? GetDriverById(string id) => _driverRepo.GetDriverById(id);
  public DeviceDriver? GetDriverByName(string name) => _driverRepo.GetDriverByName(name);
  public int Count => _driverRepo.Cache.Count;
  public IEnumerator<DeviceDriver> GetEnumerator() => _driverRepo.GetEnumerator();
  IEnumerator IEnumerable.GetEnumerator() => _driverRepo.GetEnumerator();
}
