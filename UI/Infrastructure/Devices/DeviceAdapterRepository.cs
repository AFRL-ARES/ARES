using DynamicData;
using DynamicData.Kernel;
using UI.Application.Devices;
using UI.Application.Devices.Repos;

namespace UI.Infrastructure.Devices;

public class DeviceAdapterRepository : IDeviceAdapterRepository
{
  private readonly SourceCache<IAresDeviceAdapter, string> _cache = new(device => device.Id);

  public Func<IAresDeviceAdapter, string> KeySelector => _cache.KeySelector;

  public int Count => _cache.Count;

  public IReadOnlyList<IAresDeviceAdapter> Items => _cache.Items;

  public IReadOnlyList<string> Keys => _cache.Keys;

  public IReadOnlyDictionary<string, IAresDeviceAdapter> KeyValues => _cache.KeyValues;

  public IObservable<int> CountChanged => _cache.CountChanged;

  public IObservable<IChangeSet<IAresDeviceAdapter, string>> Connect(Func<IAresDeviceAdapter, bool>? predicate = null, bool suppressEmptyChangeSets = true)
  {
    return _cache.Connect(predicate, suppressEmptyChangeSets);
  }

  public void Dispose()
  {
    _cache.Dispose();
  }

  public void Edit(Action<ISourceUpdater<IAresDeviceAdapter, string>> updateAction)
  {
    _cache.Edit(updateAction);
  }

  public Optional<IAresDeviceAdapter> Lookup(string key)
  {
    return _cache.Lookup(key);
  }

  public IObservable<IChangeSet<IAresDeviceAdapter, string>> Preview(Func<IAresDeviceAdapter, bool>? predicate = null)
  {
    return _cache.Preview(predicate);
  }

  public IObservable<Change<IAresDeviceAdapter, string>> Watch(string key)
  {
    return _cache.Watch(key);
  }
}
