using DynamicData;
using DynamicData.Kernel;
using UI.Application.Devices.Repos;

namespace UI.Infrastructure.Devices.Sila;

public class SilaDeviceAdapterRepository : ISilaDeviceAdapterRepository
{
  private readonly SourceCache<SilaDeviceAdapter, string> _cache = new(device => device.Id);
  public Func<SilaDeviceAdapter, string> KeySelector => _cache.KeySelector;
  public int Count => _cache.Count;
  public IReadOnlyList<SilaDeviceAdapter> Items => _cache.Items;

  public IReadOnlyList<string> Keys => _cache.Keys;

  public IReadOnlyDictionary<string, SilaDeviceAdapter> KeyValues => _cache.KeyValues;

  public IObservable<int> CountChanged => _cache.CountChanged;

  public IObservable<IChangeSet<SilaDeviceAdapter, string>> Connect(Func<SilaDeviceAdapter, bool>? predicate = null, bool suppressEmptyChangeSets = true)
  {
    return _cache.Connect(predicate, suppressEmptyChangeSets);
  }

  public void Dispose()
  {
    _cache.Dispose();
  }

  public void Edit(Action<ISourceUpdater<SilaDeviceAdapter, string>> updateAction)
  {
    _cache.Edit(updateAction);
  }

  public Optional<SilaDeviceAdapter> Lookup(string key)
  {
    return _cache.Lookup(key);
  }

  public IObservable<IChangeSet<SilaDeviceAdapter, string>> Preview(Func<SilaDeviceAdapter, bool>? predicate = null)
  {
    return _cache.Preview(predicate);
  }

  public IObservable<Change<SilaDeviceAdapter, string>> Watch(string key)
  {
    return _cache.Watch(key);
  }
}