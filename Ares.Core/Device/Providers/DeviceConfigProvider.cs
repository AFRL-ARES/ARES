using Ares.Core.Device.Repos;
using Ares.Datamodel.Device;
using DynamicData;
using System.Collections;

namespace Ares.Core.Device.Providers;

public class DeviceConfigProvider : IDeviceConfigProvider
{
  private readonly IDeviceConfigRepo _configRepo;

  public DeviceConfigProvider(IDeviceConfigRepo repo)
  {
    _configRepo = repo;
  }

  public int Count => _configRepo.Cache.Count;
  public IObservable<IChangeSet<DeviceConfig, string>> Connect() => _configRepo.Cache.Connect();
  public IReadOnlyCollection<DeviceConfig> GetAllConfigs() => _configRepo.GetAll();
  public DeviceConfig? GetConfig(string id) => _configRepo.GetConfig(id);
  public IEnumerator<DeviceConfig> GetEnumerator() => _configRepo.GetEnumerator();
  IEnumerator IEnumerable.GetEnumerator() => _configRepo.GetEnumerator();
}
