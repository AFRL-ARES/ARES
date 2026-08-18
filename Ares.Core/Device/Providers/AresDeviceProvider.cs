using Ares.Core.Device.Repos;
using Ares.Device;
using DynamicData;
using System.Collections;

namespace Ares.Core.Device.Providers;

/// <summary>
/// Mediates Read-Only access to the ARES device repository.
/// </summary>
public class AresDeviceProvider : IAresDeviceProvider
{
  private readonly IAresDeviceRepo _deviceRepo;

  public AresDeviceProvider(IAresDeviceRepo deviceRepo)
  {
    _deviceRepo = deviceRepo;
  }

  public IReadOnlyCollection<T> GetAllDevices<T>() where T : IAresDevice => _deviceRepo.GetAll<T>();
  public IReadOnlyCollection<IAresDevice> GetAllDevices() => _deviceRepo.GetAll();
  public IObservable<IChangeSet<IAresDevice, string>> Connect() => _deviceRepo.Cache.Connect();
  public IAresDevice? GetDevice(string id) => _deviceRepo.GetDevice(id);
  public T? GetDevice<T>(string id) where T : class, IAresDevice => _deviceRepo.GetDevice<T>(id);
  public int Count => _deviceRepo.Cache.Count;
  public IEnumerator<IAresDevice> GetEnumerator() => _deviceRepo.GetEnumerator();
  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
