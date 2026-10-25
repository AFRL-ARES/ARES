using DynamicData;
using DynamicData.Kernel;
using UI.Application.Devices.Repos;

namespace UI.Infrastructure.Devices.Remote;

public class RemoteDeviceAdapterRepository : IRemoteDeviceAdapterRepository
{
  private readonly SourceCache<RemoteDeviceAdapter, string> _cache = new(device => device.Id);

  public Func<RemoteDeviceAdapter, string> KeySelector => _cache.KeySelector;

  public int Count => _cache.Count;

  public IReadOnlyList<RemoteDeviceAdapter> Items => _cache.Items;

  public IReadOnlyList<string> Keys => _cache.Keys;

  public IReadOnlyDictionary<string, RemoteDeviceAdapter> KeyValues => _cache.KeyValues;

  public IObservable<int> CountChanged => _cache.CountChanged;

  public IObservable<IChangeSet<RemoteDeviceAdapter, string>> Connect(Func<RemoteDeviceAdapter, bool>? predicate = null, bool suppressEmptyChangeSets = true)
  {
    return _cache.Connect(predicate, suppressEmptyChangeSets);
  }

  public void Dispose()
  {
    _cache.Dispose();
  }

  public void Edit(Action<ISourceUpdater<RemoteDeviceAdapter, string>> updateAction)
  {
    _cache.Edit(updateAction);
  }

  public Optional<RemoteDeviceAdapter> Lookup(string key)
  {
    return _cache.Lookup(key);
  }

  public IObservable<IChangeSet<RemoteDeviceAdapter, string>> Preview(Func<RemoteDeviceAdapter, bool>? predicate = null)
  {
    return _cache.Preview(predicate);
  }

  public IObservable<Change<RemoteDeviceAdapter, string>> Watch(string key)
  {
    return _cache.Watch(key);
  }
}
