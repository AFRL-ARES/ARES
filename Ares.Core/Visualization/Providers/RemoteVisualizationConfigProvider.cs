using Ares.Core.Visualization.Repos;
using Ares.Datamodel.Visualizing.Remote;
using DynamicData;
using System.Collections;

namespace Ares.Core.Visualization.Providers;

public sealed class RemoteVisualizationConfigProvider : IRemoteVisualizationConfigProvider
{
  private readonly IRemoteVisualizationConfigRepo _repo;

  public RemoteVisualizationConfigProvider(IRemoteVisualizationConfigRepo repo)
  {
    _repo = repo;
  }

  public IObservable<IChangeSet<RemoteVisualizationConfig, string>> Connect()
    => _repo.Cache.Connect();

  public RemoteVisualizationConfig? GetConfig(string id)
    => _repo.GetConfig(id);

  public IReadOnlyCollection<RemoteVisualizationConfig> GetAllConfigs()
    => _repo.GetAll();

  public IEnumerator<RemoteVisualizationConfig> GetEnumerator()
    => _repo.GetEnumerator();

  IEnumerator IEnumerable.GetEnumerator()
    => GetEnumerator();
}