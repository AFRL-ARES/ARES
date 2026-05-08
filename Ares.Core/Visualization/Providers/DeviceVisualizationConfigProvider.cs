using Ares.Core.Visualization.Repos;
using Ares.Datamodel.Visualizing.Local;
using DynamicData;
using System.Collections;

namespace Ares.Core.Visualization.Providers;

public class DeviceVisualizationConfigProvider : IDeviceVisualizationConfigProvider
{
  private readonly IDeviceVisualizationConfigRepo _repo;

  public DeviceVisualizationConfigProvider(IDeviceVisualizationConfigRepo repo)
  {
    _repo = repo;
  }
  
  public int Count => _repo.Cache.Count;
  public IObservable<IChangeSet<DeviceVisualizationConfig, string>> Connect() => _repo.Cache.Connect();
  public IReadOnlyCollection<DeviceVisualizationConfig> GetAllConfigs() => _repo.GetAll();
  public DeviceVisualizationConfig? GetConfig(string id) => _repo.GetConfig(id);
  public IEnumerable<DeviceVisualizationConfig> GetConfigsByDeviceId(string deviceId) => _repo.GetConfigsByDeviceId(deviceId);
  public IEnumerator<DeviceVisualizationConfig> GetEnumerator() => _repo.GetEnumerator();
  IEnumerator IEnumerable.GetEnumerator() => _repo.GetEnumerator();
}
